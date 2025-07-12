#if !defined(_MSC_VER) || _MSVC_LANG < 201703L
#error "Please compile using MSVC and C++17 standard."
#endif

#include <cstdio>
#include <cstdint>
#include <cstdlib>
#include <cstdarg>
#include <cstring>
#include <cmath>

#include <memory>
#include <set>
#include <sstream>

#include "rapidfuzz/fuzz.hpp"
namespace fuzz = rapidfuzz::fuzz;

#include <windows.h>
#include <dwmapi.h>
#include <psapi.h>
#include <d3d11.h>
#include <dxgi1_2.h>
#include <d3dcompiler.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Graphics.Capture.h>
#include <Windows.Graphics.Capture.Interop.h>
#include <windows.graphics.directx.direct3d11.interop.h>

using std::shared_ptr, std::make_shared, winrt::com_ptr;

namespace WRT = winrt;
namespace WF = WRT::Windows::Foundation;
namespace WG = WRT::Windows::Graphics;
namespace WGC = WG::Capture;
namespace WGDX = WG::DirectX;
namespace WGD3D11 = WGDX::Direct3D11;
using Windows::Graphics::DirectX::Direct3D11::IDirect3DDxgiInterfaceAccess;

#include "libretro.h"
#include "vulkan/vulkan_symbol_wrapper.h"
#include "libretro_vulkan.h"
#include <vulkan/vulkan_win32.h>

#define RETRO_VARIABLE(x) \
retro_variable (x) = {}; (x).key = #x; \
environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &x); \
if ((x).value == NULL) { log_cb(RETRO_LOG_WARN, "Can't get value of variable %s, so nulling. Expect the unexpected!\n", #x); (x).value = ""; }

struct retro_log_callback logging;
static retro_log_printf_t log_cb;
static retro_environment_t environ_cb;
static retro_video_refresh_t video_cb;
static retro_audio_sample_t audio_cb;
static retro_audio_sample_batch_t audio_batch_cb;
static retro_input_poll_t input_poll_cb;
static retro_input_state_t input_state_cb;
static char retro_system_directory[4096];
static retro_game_geometry game_geom_saved = {};
static unsigned frame_counter;

#define NOMINAL_VIDEO_WIDTH 512
#define NOMINAL_VIDEO_HEIGHT 384
#define MAX_VIDEO_WIDTH 4096
#define MAX_VIDEO_HEIGHT 4096

#define WINDOW_RECHECK_INTERVAL 40 // measured in frames, 24fps = 1/6th of a second, 60fps = 1/15th of a second, which seems reasonable
#define TOPMOST_HOTKEY_ID 42069

static retro_hw_render_callback hw_render;
static const retro_hw_render_interface_vulkan* vulkan;
static uint32_t vulkan_mainqf_idx;
static uint32_t vulkan_presentqf_idx;

#define VULKAN_API_VERSION VK_API_VERSION_1_1
#define VULKAN_MAX_SYNC 8

struct VulkanRenderState
{
    bool swapchainValid;

    unsigned syncIdx;
    unsigned swapchainImageCount;
    uint32_t swapchainMask;

    VkPhysicalDeviceMemoryProperties memoryProperties;
    VkPhysicalDeviceProperties gpuProperties;

    retro_vulkan_image swapImages[VULKAN_MAX_SYNC];
    VkDeviceMemory swapImageMemory[VULKAN_MAX_SYNC];
    VkImage d3dImage;
    VkDeviceMemory d3dImageMemory;

    ID3D11Texture2D* d3dStoredTexture;
    D3D11_TEXTURE2D_DESC d3dStoredDesc;
    HANDLE d3dTextureHandle;

    VkCommandPool commandPool[VULKAN_MAX_SYNC];
    VkCommandBuffer command[VULKAN_MAX_SYNC];

    uint32_t IdentifyMemoryType(uint32_t deviceReqs, uint32_t hostReqs)
    {
        const VkPhysicalDeviceMemoryProperties* props = &this->memoryProperties;
        for (unsigned i = 0; i < VK_MAX_MEMORY_TYPES; i++)
        {
            if (deviceReqs & (1u << i))
            {
                if ((props->memoryTypes[i].propertyFlags & hostReqs) == hostReqs)
                {
                    return i;
                }
            }
        }
        return 0;
    }

    VulkanRenderState(): swapchainValid(false) {
        // Get GPU properties and memory properties.
        vkGetPhysicalDeviceProperties(vulkan->gpu, &this->gpuProperties);
        vkGetPhysicalDeviceMemoryProperties(vulkan->gpu, &this->memoryProperties);

        // Figure out how many swapchain images we have, and save the swapchain mask.
        unsigned imageCount = 0;
        uint32_t mask = vulkan->get_sync_index_mask(vulkan->handle);
        for (unsigned i = 0; i < 32; i++)
            if (mask & (1u << i))
                imageCount = i + 1;
        this->swapchainImageCount = imageCount;
        this->swapchainMask = mask;

        // Create our command pool and command buffers.
        VkCommandPoolCreateInfo poolInfo = { VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO };
        VkCommandBufferAllocateInfo bufferInfo = { VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO };
        poolInfo.queueFamilyIndex = vulkan->queue_index;
        poolInfo.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
        for (unsigned i = 0; i < this->swapchainImageCount; i++)
        {
            vkCreateCommandPool(vulkan->device, &poolInfo, NULL, &this->commandPool[i]);
            bufferInfo.commandPool = this->commandPool[i];
            bufferInfo.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
            bufferInfo.commandBufferCount = 1;
            vkAllocateCommandBuffers(vulkan->device, &bufferInfo, &this->command[i]);
        }
    }

    void DestroyImageStructures()
    {
        if (!swapchainValid)
            return;

        // Cleanup D3D image structures.
        vkFreeMemory(vulkan->device, this->d3dImageMemory, NULL);
        vkDestroyImage(vulkan->device, this->d3dImage, NULL);

        // Cleanup swapchain image structures.
        for (unsigned i = 0; i < this->swapchainImageCount; i++)
        {
            //vkDestroyFramebuffer(vulkan->device, this->framebuffers[i], NULL);
            vkDestroyImageView(vulkan->device, this->swapImages[i].image_view, NULL);
            vkFreeMemory(vulkan->device, this->swapImageMemory[i], NULL);
            vkDestroyImage(vulkan->device, this->swapImages[i].create_info.image, NULL);
        }
    }

    ~VulkanRenderState()
    {
        if (!vulkan)
            return;

        // Wait for device to go idle before cleanup.
        vkDeviceWaitIdle(vulkan->device);

        this->DestroyImageStructures();

        // Cleanup render pass and pipeline.
        //vkDestroyRenderPass(vulkan->device, this->renderPass, NULL);

        // Cleanup command buffers and pools.
        for (unsigned i = 0; i < this->swapchainImageCount; i++)
        {
            vkFreeCommandBuffers(vulkan->device, this->commandPool[i], 1, &this->command[i]);
            vkDestroyCommandPool(vulkan->device, this->commandPool[i], NULL);
        }
    }

    void SetupSwapchain()
    {
        this->DestroyImageStructures();

        // (Re)-create images.
        for (unsigned i = 0; i < this->swapchainImageCount; i++)
        {
            // Create swapchain images.
            VkImageCreateInfo swapImageInfo = { VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO };
            swapImageInfo.imageType = VK_IMAGE_TYPE_2D;
            swapImageInfo.flags |= VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT;
            swapImageInfo.format = VK_FORMAT_R8G8B8A8_UNORM;
            swapImageInfo.extent.width = this->d3dStoredDesc.Width;
            swapImageInfo.extent.height = this->d3dStoredDesc.Height;
            swapImageInfo.extent.depth = 1;
            swapImageInfo.samples = VK_SAMPLE_COUNT_1_BIT;
            swapImageInfo.tiling = VK_IMAGE_TILING_OPTIMAL;
            swapImageInfo.usage = VK_IMAGE_USAGE_SAMPLED_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT;
            swapImageInfo.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
            swapImageInfo.mipLevels = 1;
            swapImageInfo.arrayLayers = 1;

            this->swapImages[i].create_info = { VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO };
            vkCreateImage(vulkan->device, &swapImageInfo, NULL, &this->swapImages[i].create_info.image);

            // Allocate memory for the swapchain images.
            VkMemoryRequirements swapMemoryReqs;
            vkGetImageMemoryRequirements(vulkan->device, this->swapImages[i].create_info.image, &swapMemoryReqs);

            VkMemoryAllocateInfo swapAllocInfo = { VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO };
            swapAllocInfo.allocationSize = swapMemoryReqs.size;
            swapAllocInfo.memoryTypeIndex = IdentifyMemoryType(
                swapMemoryReqs.memoryTypeBits, VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);

            vkAllocateMemory(vulkan->device, &swapAllocInfo, NULL, &this->swapImageMemory[i]);
            vkBindImageMemory(vulkan->device, this->swapImages[i].create_info.image, this->swapImageMemory[i], 0);

            // Create image views.
            this->swapImages[i].create_info.viewType = VK_IMAGE_VIEW_TYPE_2D;
            this->swapImages[i].create_info.format = VK_FORMAT_R8G8B8A8_UNORM;
            this->swapImages[i].create_info.subresourceRange.baseMipLevel = 0;
            this->swapImages[i].create_info.subresourceRange.baseArrayLayer = 0;
            this->swapImages[i].create_info.subresourceRange.levelCount = 1;
            this->swapImages[i].create_info.subresourceRange.layerCount = 1;
            this->swapImages[i].create_info.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
            this->swapImages[i].create_info.components.r = VK_COMPONENT_SWIZZLE_R;
            this->swapImages[i].create_info.components.g = VK_COMPONENT_SWIZZLE_G;
            this->swapImages[i].create_info.components.b = VK_COMPONENT_SWIZZLE_B;
            this->swapImages[i].create_info.components.a = VK_COMPONENT_SWIZZLE_A;

            vkCreateImageView(vulkan->device, &this->swapImages[i].create_info, NULL, &this->swapImages[i].image_view);
            this->swapImages[i].image_layout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
            
        }

        // Create D3D-backed image.
        VkExternalMemoryImageCreateInfo extMemoryInfo = { VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO };
        extMemoryInfo.handleTypes = VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT;
        VkImageCreateInfo d3dImageInfo = { VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO };
        d3dImageInfo.pNext = &extMemoryInfo;
        d3dImageInfo.imageType = VK_IMAGE_TYPE_2D;
        d3dImageInfo.format = VK_FORMAT_B8G8R8A8_UNORM;
        d3dImageInfo.extent.width = this->d3dStoredDesc.Width;
        d3dImageInfo.extent.height = this->d3dStoredDesc.Height;
        d3dImageInfo.extent.depth = 1;
        d3dImageInfo.samples = VK_SAMPLE_COUNT_1_BIT;
        d3dImageInfo.tiling = VK_IMAGE_TILING_OPTIMAL;
        d3dImageInfo.usage = VK_IMAGE_USAGE_TRANSFER_SRC_BIT;
        d3dImageInfo.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
        d3dImageInfo.mipLevels = 1;
        d3dImageInfo.arrayLayers = 1;

        vkCreateImage(vulkan->device, &d3dImageInfo, NULL, &this->d3dImage);

        // Create and store handle and keyed mutex for D3D11 texture.
        IDXGIResource1* d3dResource;
        d3dStoredTexture->QueryInterface(__uuidof(IDXGIResource1), (void**)&d3dResource);
        d3dResource->CreateSharedHandle(NULL, DXGI_SHARED_RESOURCE_READ, NULL, &d3dTextureHandle);

        // Work out our memory allocation requirements.
        PFN_vkGetImageMemoryRequirements2 vkGetImageMemoryRequirements2;
        vulkan_symbol_wrapper_load_device_symbol(vulkan->device, "vkGetImageMemoryRequirements2", (PFN_vkVoidFunction*)&vkGetImageMemoryRequirements2);
        VkMemoryDedicatedRequirements memoryDedicatedReqs = { VK_STRUCTURE_TYPE_MEMORY_DEDICATED_REQUIREMENTS };
        VkMemoryRequirements2 d3dMemoryReqs = { VK_STRUCTURE_TYPE_MEMORY_REQUIREMENTS_2 };
        d3dMemoryReqs.pNext = &memoryDedicatedReqs;

        VkImageMemoryRequirementsInfo2 d3dMemoryReqsInfo = { VK_STRUCTURE_TYPE_IMAGE_MEMORY_REQUIREMENTS_INFO_2 };
        d3dMemoryReqsInfo.image = this->d3dImage;
        vkGetImageMemoryRequirements2(vulkan->device, &d3dMemoryReqsInfo, &d3dMemoryReqs);

        // Work out the memory type required for the handle.
        PFN_vkGetMemoryWin32HandlePropertiesKHR vkGetMemoryWin32HandlePropertiesKHR;
        vulkan_symbol_wrapper_load_device_symbol(vulkan->device, "vkGetMemoryWin32HandlePropertiesKHR", (PFN_vkVoidFunction*)&vkGetMemoryWin32HandlePropertiesKHR);
        VkMemoryWin32HandlePropertiesKHR handleProps = { VK_STRUCTURE_TYPE_MEMORY_WIN32_HANDLE_PROPERTIES_KHR };
        vkGetMemoryWin32HandlePropertiesKHR(vulkan->device, VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT, d3dTextureHandle, &handleProps);

        // Now actually allocate/import the memory.
        VkMemoryDedicatedAllocateInfo dedicatedAllocInfo = { VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO };
        dedicatedAllocInfo.image = this->d3dImage;
        VkImportMemoryWin32HandleInfoKHR importInfo = { VK_STRUCTURE_TYPE_IMPORT_MEMORY_WIN32_HANDLE_INFO_KHR };
        importInfo.pNext = &dedicatedAllocInfo;
        importInfo.handleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT;
        importInfo.handle = d3dTextureHandle;
        VkMemoryAllocateInfo allocInfo = { VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO };
        allocInfo.pNext = &importInfo;
        allocInfo.memoryTypeIndex = IdentifyMemoryType(
            handleProps.memoryTypeBits, VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);

        vkAllocateMemory(vulkan->device, &allocInfo, NULL, &this->d3dImageMemory);
        vkBindImageMemory(vulkan->device, this->d3dImage, this->d3dImageMemory, 0);

        swapchainValid = true;
    }

    void BlitD3D11Texture(ID3D11Device* device, ID3D11Texture2D* texture)
    {
        // Get a description and shared handle relevant to the D3D11 texture.
        D3D11_TEXTURE2D_DESC newTextureDesc;
        texture->GetDesc(&newTextureDesc);

        if (d3dStoredDesc.Width != newTextureDesc.Width ||
            d3dStoredDesc.Height != newTextureDesc.Height ||
            d3dStoredDesc.Format != newTextureDesc.Format ||
            d3dStoredTexture != texture)
        {
            // We only support one texture format from D3D11's side.
            if (newTextureDesc.Format != DXGI_FORMAT_R8G8B8A8_UNORM)
            {
                log_cb(RETRO_LOG_ERROR, "Can't use Vulkan to blit D3D11 texture with format other than DXGI_FORMAT_R8G8B8A8_UNORM!\n");
                return;
            }

            // Store the texture and make sure we have an up-to-date texture description.
            d3dStoredTexture = texture;
            texture->GetDesc(&d3dStoredDesc);
            this->SetupSwapchain();
        }

        // Get the appropriate command buffer for our sync index.
        this->syncIdx = vulkan->get_sync_index(vulkan->handle);
        VkCommandBuffer localCmd = this->command[this->syncIdx];

        // Start recording command buffer for this frame.
        VkCommandBufferBeginInfo beginInfo = { VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO };
        beginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
        vkResetCommandBuffer(localCmd, 0);
        vkBeginCommandBuffer(localCmd, &beginInfo);

        // Setup common subresoruce range and layers structures.
        VkImageSubresourceRange subresourceRange;
        subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
        subresourceRange.baseArrayLayer = 0;
        subresourceRange.baseMipLevel = 0;
        subresourceRange.layerCount = 1;
        subresourceRange.levelCount = 1;
        VkImageSubresourceLayers subresourceLayers;
        subresourceLayers.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
        subresourceLayers.baseArrayLayer = 0;
        subresourceLayers.mipLevel = 0;
        subresourceLayers.layerCount = 1;

        // COMMMAND - Transition the swapchain image memory for transfer destination.
        VkImageMemoryBarrier swapPreCopyBar = {VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER};
        swapPreCopyBar.srcAccessMask = 0;
        swapPreCopyBar.dstAccessMask = VK_ACCESS_TRANSFER_READ_BIT | VK_ACCESS_TRANSFER_WRITE_BIT;
        swapPreCopyBar.oldLayout = VK_IMAGE_LAYOUT_UNDEFINED;
        swapPreCopyBar.newLayout = VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
        swapPreCopyBar.srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
        swapPreCopyBar.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
        swapPreCopyBar.image = this->swapImages[this->syncIdx].create_info.image;
        swapPreCopyBar.subresourceRange = subresourceRange;
        vkCmdPipelineBarrier(localCmd, VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, VK_PIPELINE_STAGE_TRANSFER_BIT, 0, 0, NULL, 0, NULL, 1, &swapPreCopyBar);

        // COMMMAND - Clear the swap image.
        VkClearColorValue clearValue;
        clearValue.float32[0] = 1.0f;
        clearValue.float32[1] = 0.25f;
        clearValue.float32[2] = 0.75f;
        clearValue.float32[3] = 1.0f;
        vkCmdClearColorImage(localCmd, this->swapImages[this->syncIdx].create_info.image, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, &clearValue, 1, &subresourceRange);

        // COMMMAND - Transition the D3D image memory for transfer destination (acquire ownership on our queue).
        VkImageMemoryBarrier d3dPreCopyBar = {VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER};
        d3dPreCopyBar.srcAccessMask = 0;
        d3dPreCopyBar.dstAccessMask = VK_ACCESS_TRANSFER_READ_BIT | VK_ACCESS_TRANSFER_WRITE_BIT;
        d3dPreCopyBar.oldLayout = VK_IMAGE_LAYOUT_UNDEFINED;
        d3dPreCopyBar.newLayout = VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
        d3dPreCopyBar.srcQueueFamilyIndex = VK_QUEUE_FAMILY_EXTERNAL;
        d3dPreCopyBar.dstQueueFamilyIndex = vulkan_mainqf_idx;
        d3dPreCopyBar.image = this->d3dImage;
        d3dPreCopyBar.subresourceRange = subresourceRange;
        vkCmdPipelineBarrier(localCmd, VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, VK_PIPELINE_STAGE_TRANSFER_BIT, 0, 0, NULL, 0, NULL, 1, &d3dPreCopyBar);

        // COMMAND - Copy from D3D image to swapchain image.
        VkImageCopy imageCopy;
        imageCopy.srcSubresource = subresourceLayers;
        imageCopy.srcOffset = { 0 };
        imageCopy.dstSubresource = subresourceLayers;
        imageCopy.dstOffset = { 0 };
        imageCopy.extent.width = this->d3dStoredDesc.Width;
        imageCopy.extent.height = this->d3dStoredDesc.Height;
        imageCopy.extent.depth = 1;
        vkCmdCopyImage(localCmd,
            this->d3dImage, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
            this->swapImages[this->syncIdx].create_info.image, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
            1, &imageCopy);

        // COMMMAND - Transition the swapchain image memory for presentation.
        VkImageMemoryBarrier prePresentBar = { VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER };
        prePresentBar.srcAccessMask = 0;
        prePresentBar.dstAccessMask = VK_ACCESS_SHADER_READ_BIT;
        prePresentBar.oldLayout = VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
        prePresentBar.newLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
        prePresentBar.srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
        prePresentBar.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
        prePresentBar.image = this->swapImages[this->syncIdx].create_info.image;
        prePresentBar.subresourceRange = subresourceRange;
        vkCmdPipelineBarrier(localCmd, VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT, VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT, 0, 0, NULL, 0, NULL, 1, &prePresentBar);

        // Close the command buffer.
        vkEndCommandBuffer(localCmd);

        // Submit the command buffer.
        vulkan->lock_queue(vulkan->handle);
        VkSubmitInfo submitInfo = { VK_STRUCTURE_TYPE_SUBMIT_INFO };
        submitInfo.commandBufferCount = 1;
        submitInfo.pCommandBuffers = &localCmd;
        vkQueueSubmit(vulkan->queue, 1, &submitInfo, NULL);
        vulkan->unlock_queue(vulkan->handle);

        // Trigger rendering by calling back to RetroArch.
        vulkan->set_image(vulkan->handle, &this->swapImages[this->syncIdx], 0, NULL, VK_QUEUE_FAMILY_EXTERNAL);
        //vulkan->set_command_buffers(vulkan->handle, 1, &this->command[this->syncIdx]);
        video_cb(RETRO_HW_FRAME_BUFFER_VALID, d3dStoredDesc.Width, d3dStoredDesc.Height, 0);
    }


};
shared_ptr<VulkanRenderState> vkState;

class WGCCapture
{
    // Stored captured window
    HWND capturedWindow;

    // WGC Interfaces
    WGC::GraphicsCaptureItem item;
    WGD3D11::IDirect3DDevice device;
    WGC::Direct3D11CaptureFramePool framePool;
    WGC::GraphicsCaptureSession session;
    WG::SizeInt32 lastSize;

    // WGC Callback Management
    WGC::Direct3D11CaptureFramePool::FrameArrived_revoker frameArrived;

    // Frame surface
    com_ptr<ID3D11Texture2D> frameSurface;
    bool frameSurfaceReady;

    // Will count up on each request of a frame.
    // Reset whenever a new frame arrives.
    // If this reaches or exceeds DEAD_CAPTURE_THRESHOLD, then we assume that the backend capture had died.
    unsigned requestsSinceLastArrival;
    const unsigned DEAD_CAPTURE_THRESHOLD = WINDOW_RECHECK_INTERVAL;

public:
    enum class CaptureReadiness { CAPTURE_NOT_READY, CAPTURE_READY, CAPTURE_DEAD };

    void OnFrameArrived(WGC::Direct3D11CaptureFramePool const& sender, WF::IInspectable const&)
    {
        // Get the last captured frame.
        const WGC::Direct3D11CaptureFrame frame = sender.TryGetNextFrame();
        const WG::SizeInt32 contentSize = frame.ContentSize();

        // Extract the frame surface and mark as ready.
        auto frameSurfaceAccess = frame.Surface().as<IDirect3DDxgiInterfaceAccess>();
        frameSurface = nullptr;
        frameSurfaceAccess->GetInterface(WRT::guid_of<ID3D11Texture2D>(), frameSurface.put_void());

        frameSurfaceReady = true;
        requestsSinceLastArrival = 0;

        // Handle resize of captured window.
        if (contentSize.Width != lastSize.Width || contentSize.Height != lastSize.Height)
        {
            framePool.Recreate(device, WGDX::DirectXPixelFormat::B8G8R8A8UIntNormalized, 2, contentSize);

            lastSize = contentSize;
        }
    }

    // Client: Call IsCaptureReady() to check if a new frame capture is available. Call CaptureTaken() after copying the data.
    CaptureReadiness IsCaptureReady()
    {
        if (!capturedWindow || !IsWindow(capturedWindow) || !IsWindowVisible(capturedWindow) || item == nullptr || requestsSinceLastArrival > DEAD_CAPTURE_THRESHOLD)
            return CaptureReadiness::CAPTURE_DEAD;
        else
            return frameSurfaceReady ? CaptureReadiness::CAPTURE_READY : CaptureReadiness::CAPTURE_NOT_READY;
    }
    void CaptureTaken() { frameSurfaceReady = false; }

    com_ptr<ID3D11Texture2D> GetFrameSurface()
    {
        if (!frameSurfaceReady)
            return nullptr;

        requestsSinceLastArrival++;
        return frameSurface;
    }

    D3D11_TEXTURE2D_DESC GetFrameSurfaceDesc()
    {
        D3D11_TEXTURE2D_DESC desc = {};

        if (!frameSurfaceReady)
            return desc;

        frameSurface->GetDesc(&desc);
        return desc;
    }

    void CropClientAreaAutomatic(D3D11_BOX& box)
    {
        static wchar_t classBuffer[1024], titleBuffer[1024];
        GetClassName(capturedWindow, classBuffer, 1024);
        GetWindowText(capturedWindow, titleBuffer, 1024);

        // Hack for AppleWin Apple II emulator. Should be running in 1x mode (use CTRL-F6).
        if (wcscmp(classBuffer, L"APPLE2FRAME") == 0) {
            box.left   += 4;
            box.right  -= 49;
            box.top    += 4;
            box.bottom -= 4;
        }

        // Hack for 86Box. This hides the menubar; the toolbar/statusbar can be disabled from the menus.
        if (wcsstr(titleBuffer, L"86Box") != 0) {
            box.top += 28;
        }
    }

    void CropClientAreaCustom(D3D11_BOX& box)
    {
        // Get fine cropping parameters.
        RETRO_VARIABLE(client_crop_left_fine);
        RETRO_VARIABLE(client_crop_right_fine);
        RETRO_VARIABLE(client_crop_top_fine);
        RETRO_VARIABLE(client_crop_bottom_fine);

        // Get coarse cropping parameters.
        RETRO_VARIABLE(client_crop_left_coarse);
        RETRO_VARIABLE(client_crop_right_coarse);
        RETRO_VARIABLE(client_crop_top_coarse);
        RETRO_VARIABLE(client_crop_bottom_coarse);

        // Adjust client box per cropping parameters.
        box.left   += (atoi(client_crop_left_fine.value)   + atoi(client_crop_left_coarse.value));
        box.right  -= (atoi(client_crop_right_fine.value)  + atoi(client_crop_right_coarse.value));
        box.top    += (atoi(client_crop_top_fine.value)    + atoi(client_crop_top_coarse.value));
        box.bottom -= (atoi(client_crop_bottom_fine.value) + atoi(client_crop_bottom_coarse.value));
    }

    D3D11_BOX GetClientSubarea()
    {
        D3D11_BOX clientBox = {};
        clientBox.front = 0;
        clientBox.back = 1;

        // Get dimensions for texture.
        D3D11_TEXTURE2D_DESC desc = GetFrameSurfaceDesc();
        uint32_t surfaceWidth = desc.Width;
        uint32_t surfaceHeight = desc.Height;

        // Check if cropping is disabled by RA core options. If it is return a client box that represents the whole texture.
        retro_variable crop_enable = {}; crop_enable.key = "crop_enable";
        environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &crop_enable);
        if (!strcmp(crop_enable.value, "false")) {
            clientBox.left = 0; clientBox.right = surfaceWidth;
            clientBox.top = 0; clientBox.bottom = surfaceHeight;
            return clientBox;
        }

        // Setting the process as per-monitor DPI aware will do this automatically,
        //	and is needed to get the right values from GetClientRect() and ClientToScreen() later.
        SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE);
        //float windowDPIFactor = GetDpiForWindow(capturedWindow) / 96.0f;

        // Get dimensions for captured window.
        // Client rect needs to be scaled per window DPI factor (this is done automcatilly based on DPI awareness context).
        // Window rect is a position on screen in raw pixels (not DPI scaled).
        RECT clientRect = {}, windowRect = {};
        GetClientRect(capturedWindow, &clientRect);
        DwmGetWindowAttribute(capturedWindow, DWMWA_EXTENDED_FRAME_BOUNDS, &windowRect, sizeof(windowRect));

        // Calculate upper left point.
        POINT upperLeft = { 0 };
        ClientToScreen(capturedWindow, &upperLeft);

        // Calculate left/top for client box.
        const uint32_t left = (upperLeft.x > windowRect.left) ? (upperLeft.x - windowRect.left) : 0;
        clientBox.left = left;

        const uint32_t top = (upperLeft.y > windowRect.top) ? (upperLeft.y - windowRect.top) : 0;
        clientBox.top = top;

        // Calculate width/height of client, and remaining dimension of client box.
        uint32_t clientWidth = 1;
        if (surfaceWidth > left)
            clientWidth = min((surfaceWidth - left), (uint32_t)clientRect.right);
        uint32_t clientHeight = 1;
        if (surfaceHeight > top)
            clientHeight = min((surfaceHeight - top), (uint32_t)clientRect.bottom);

        clientBox.right = left + clientWidth;
        clientBox.bottom = top + clientHeight;

        // Check if we need to perform additional cropping.
        RETRO_VARIABLE(client_crop_mode);
        if (!strcmp(client_crop_mode.value, "auto"))
            CropClientAreaAutomatic(clientBox);
        else if (!strcmp(client_crop_mode.value, "custom")) {
            CropClientAreaCustom(clientBox);
        }

        // Check sanity of client box (clamp to sensible values).
        clientBox.left = max(0, min(surfaceWidth, clientBox.left)); // left in range of texture
        clientBox.right  = max(0, min(surfaceWidth, clientBox.right)); // right in range of texture
        clientBox.top    = max(0, min(surfaceHeight, clientBox.top)); // top in range of texture
        clientBox.bottom = max(0, min(surfaceHeight, clientBox.bottom)); // bottom in range of texture
        clientBox.left   = min(clientBox.right - 1, clientBox.left); // left somewhat smaller than right
        clientBox.top    = min(clientBox.bottom - 1, clientBox.top); // top somewhat smaller than bottom

        return clientBox;
    }

    WGCCapture(const com_ptr<IDXGIDevice>& dxgiDevice, HWND inCapturedWindow)
        : capturedWindow(inCapturedWindow), requestsSinceLastArrival(0),
        item(nullptr), device(nullptr), framePool(nullptr), session(nullptr), frameSurfaceReady(false), lastSize()
    {
        assert(WGC::GraphicsCaptureSession::IsSupported()); // Assert WGC support
        if (!capturedWindow || !IsWindow(capturedWindow) || !IsWindowVisible(capturedWindow))
            return;

        // Get a GraphicsCaptureItem for the window.
        // This feels kind of hacky, but is indeed the 'correct' way to do this.

        auto activationFactory = WRT::get_activation_factory<WGC::GraphicsCaptureItem>();
        auto gciInterop = activationFactory.as<IGraphicsCaptureItemInterop>();
        gciInterop->CreateForWindow(capturedWindow, WRT::guid_of<WGC::GraphicsCaptureItem>(), (void**)WRT::put_abi(item));

        // Get a WinRT D3D11 device from given DXGI device.

        WRT::com_ptr<IInspectable> inspectable;
        CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.get(), inspectable.put());
        device = inspectable.as<WGD3D11::IDirect3DDevice>();

        // Sanity check for a valid GraphicsCaptureItem before we proceed further.
        // If this check fails, most likely the window died a short death, and next call to IsCaptureReady() will confirm this.
        // For now, just take an early bath before the code below goes and does something silly and crashes the core.
        if (item == nullptr)
            return;

        // Create capture frame pool and session, and register callbacks.

        framePool = WGC::Direct3D11CaptureFramePool::Create(device, WGDX::DirectXPixelFormat::B8G8R8A8UIntNormalized, 2, item.Size());
        session = framePool.CreateCaptureSession(item);
        lastSize = item.Size();
        frameArrived = framePool.FrameArrived(WRT::auto_revoke, { this, &WGCCapture::OnFrameArrived });

        // Start the capture.

        session.StartCapture();
    }
};

class Direct3DShaderPass
{
    enum class SHADER_TYPE
    {
        ST_VERTEXSHADER,
        ST_PIXELSHADER
    };

    // Direct3D Overall Interfaces
    com_ptr<ID3D11Device> device;
    com_ptr<ID3D11DeviceContext> deviceContext;

    // Shader structures
    com_ptr<ID3D11InputLayout> inputLayout;
    com_ptr<ID3D11VertexShader> vs;
    com_ptr<ID3D11PixelShader> ps;

    static com_ptr<ID3D10Blob> CompileShader(const char* shaderSource, const SHADER_TYPE shaderType)
    {
        HRESULT hr = 0;
        com_ptr<ID3D10Blob> codeBlob, errBlob;
        switch (shaderType) {
        case SHADER_TYPE::ST_VERTEXSHADER:
            hr = D3DCompile(shaderSource, strlen(shaderSource), nullptr, nullptr, nullptr, "main_vs", "vs_4_0", 0, 0, codeBlob.put(), errBlob.put());
            break;
        case SHADER_TYPE::ST_PIXELSHADER:
            hr = D3DCompile(shaderSource, strlen(shaderSource), nullptr, nullptr, nullptr, "main_ps", "ps_4_0", 0, 0, codeBlob.put(), errBlob.put());
            break;
        }

        if (FAILED(hr)) {
            log_cb(RETRO_LOG_ERROR, "Failed to compile HLSL shader. Error log written.\n");
        }

        // Write out the compile log if it exists -- it might have contained warnings.
        if (errBlob) {
            std::wostringstream wos;
            switch (shaderType) {
            case SHADER_TYPE::ST_VERTEXSHADER:
                wos << "vs.compile.log";
                break;
            case SHADER_TYPE::ST_PIXELSHADER:
                wos << "ps.compile.log";
                break;
            }
            D3DWriteBlobToFile(errBlob.get(), wos.str().c_str(), TRUE);
        }

        return codeBlob;
    }
public:
    Direct3DShaderPass(const com_ptr<ID3D11Device>& inDevice,
        const com_ptr<ID3D11DeviceContext>& inDeviceContext,
        const char* vsSource, const char* psSource,
        const D3D11_INPUT_ELEMENT_DESC ied[],
        const size_t iedSize)
        : device(inDevice), deviceContext(inDeviceContext)
    {
        // Create shader blobs and shader objects.
        com_ptr<ID3D10Blob> vsBlob = CompileShader(vsSource, SHADER_TYPE::ST_VERTEXSHADER);
        com_ptr<ID3D10Blob> psBlob = CompileShader(psSource, SHADER_TYPE::ST_PIXELSHADER);

        if (!vsBlob || !psBlob) {
            environ_cb(RETRO_ENVIRONMENT_SHUTDOWN, NULL);
            return;
        }

        this->device->CreateVertexShader(vsBlob->GetBufferPointer(), vsBlob->GetBufferSize(), nullptr,
            this->vs.put());
        this->device->CreatePixelShader(psBlob->GetBufferPointer(), psBlob->GetBufferSize(), nullptr,
            this->ps.put());

        // Set up input layout.
        this->device->CreateInputLayout(ied, iedSize, vsBlob->GetBufferPointer(), vsBlob->GetBufferSize(),
            this->inputLayout.put());
    }

    void Activate()
    {
        this->deviceContext->IASetInputLayout(this->inputLayout.get());
        this->deviceContext->VSSetShader(this->vs.get(), nullptr, 0);
        this->deviceContext->PSSetShader(this->ps.get(), nullptr, 0);
    }
};

struct CaptureState {
    typedef struct
    {
        std::wstring partial;
        HWND window;
        double score;
        double priority;
    } SCORED_WINDOW;

    std::vector<std::wstring> capturePartials;
    std::string uploadMethod;
    HWND captureWindow;
    shared_ptr<WGCCapture> capture;

    com_ptr<ID3D11Device> device;
    com_ptr<ID3D11DeviceContext> deviceContext;
    com_ptr<IDXGIDevice> dxgiDevice;

    com_ptr<ID3D11Texture2D> croppedTexture;
    D3D11_TEXTURE2D_DESC croppedStoredDesc;
    com_ptr<ID3D11ShaderResourceView> croppedView;

    com_ptr<ID3D11Texture2D> convertedTexture;
    com_ptr<IDXGIKeyedMutex> convertedMutex;
    D3D11_TEXTURE2D_DESC convertedStoredDesc;
    com_ptr<ID3D11RenderTargetView> convertedTarget;
    D3D11_VIEWPORT convertedViewport;

    com_ptr<ID3D11Texture2D> readbackTexture;

    typedef struct
    {
        FLOAT x, y;
        FLOAT uv[2];
    } VERTEX;

    shared_ptr<Direct3DShaderPass> convertPass;
    com_ptr<ID3D11SamplerState> linearSampler;
    com_ptr<ID3D11SamplerState> pointSampler;
    com_ptr<ID3D11Buffer> fqVertexBuffer;
    com_ptr<ID3D11Buffer> fqIndexBuffer;

    bool convertReady;

    HWND dummyHotkeyWindow;
    bool topmostMode;
    HWND topmostedWindow;

    CaptureState(const std::vector<std::wstring>& inCapturePartials, const char* inUploadMethod)
        : capturePartials(inCapturePartials), uploadMethod(inUploadMethod),
        convertReady(false), topmostMode(false), topmostedWindow(NULL),
        captureWindow(NULL), dummyHotkeyWindow(NULL) {
        // Create the D3D11 device and find DXGI device for WGC.
        HRESULT hr = D3D11CreateDevice(
            nullptr,
            D3D_DRIVER_TYPE_HARDWARE,
            nullptr,
            0,
            nullptr,
            0,
            D3D11_SDK_VERSION,
            this->device.put(),
            nullptr,
            this->deviceContext.put()
        );
        if (FAILED(hr)) {
            log_cb(RETRO_LOG_ERROR, "Failed to initialize D3D11 for WGC core, with error code: %ld\n", hr);
            environ_cb(RETRO_ENVIRONMENT_SHUTDOWN, NULL);
            return;
        }
        device->QueryInterface(dxgiDevice.put());

        // Setup render pass for texture format conversion.
        SetupConvertPass();
    }

    ~CaptureState() {
        CleanupTopmostHack();
    }

    const HWND AttemptFindCaptureWindow()
    {
        // Try and select capture window per one of our known partials.
        // Most of the fuzzy logic is in the callback.
        HWND returnWindow = NULL;
        std::vector<SCORED_WINDOW> scoredWindows;
        for (auto capturePartial : capturePartials) {
            if (capturePartial.empty())
                continue;

            // Extract priority from capturePartial if present.
            double priority = 1.0;
            std::wstring actualPartial;
            if (capturePartial.find_last_of(L"[") != std::wstring::npos) {
                const wchar_t* whitespaces = L" \n\t\r\f\v";
                std::wstring priorityString = capturePartial.substr(capturePartial.find_last_of(L"[") + 1);
                priority = max(1.0, wcstof(priorityString.c_str(), NULL));
                actualPartial = capturePartial.substr(0, capturePartial.find_last_of(L"["));
                actualPartial.erase(actualPartial.find_last_not_of(whitespaces) + 1); // Strip trailing whitespace.
            } else {
                actualPartial = std::wstring(capturePartial);
            }

            SCORED_WINDOW newScoredWindow = {};
            newScoredWindow.partial = actualPartial;
            newScoredWindow.priority = priority;
            EnumWindows(CaptureState::EnumWindowProc, LPARAM(&newScoredWindow));

            // Sanity check -- is the newly scored window actually a visible window?
            if (newScoredWindow.window && IsWindow(newScoredWindow.window) && IsWindowVisible(newScoredWindow.window))
                scoredWindows.push_back(newScoredWindow);
        }

        // Sort our list of scored windows and return the best scoring one if we have it.
        std::sort(scoredWindows.begin(), scoredWindows.end(), [](SCORED_WINDOW a, SCORED_WINDOW b) {
            return ( a.score * a.priority ) > ( b.score * b.priority ) ;
            });
        if (scoredWindows.size() >= 1)
            returnWindow = scoredWindows[0].window;

        return returnWindow;
    }

    bool SetupCaptureContext()
    {
        // Delete our existing capture context if we have one.
        if (capture)
            capture.reset();

        // Find a capture window at this point, or we should just return and say we couldn't find one.
        HWND newCaptureWindow = AttemptFindCaptureWindow();
        if (newCaptureWindow == NULL)
            return false;
        captureWindow = newCaptureWindow;

        // Setup WGC capture context.
        capture = make_shared<WGCCapture>(this->dxgiDevice, captureWindow);

        return true;
    }

    void UpdateCaptureTextures()
    {
        D3D11_BOX sourceBox = capture->GetClientSubarea();
        UINT newSourceWidth = (sourceBox.right - sourceBox.left);
        UINT newSourceHeight = (sourceBox.bottom - sourceBox.top);
        ID3D11Texture2D* newSourceTexture = capture->GetFrameSurface().get();
        D3D11_TEXTURE2D_DESC newTextureDesc = capture->GetFrameSurfaceDesc();

        // Handle scaling per parameters. Initially we assume the downscale resolution will match the source resolution.
        UINT downscaleWidth = newSourceWidth;
        UINT downscaleHeight = newSourceHeight;
        RETRO_VARIABLE(scale_enable);
        if (!strcmp(scale_enable.value, "exact")) {
            RETRO_VARIABLE(scale_res_w);
            RETRO_VARIABLE(scale_res_h);
            if(strcmp(scale_res_w.value, "source"))
                downscaleWidth = atoi(scale_res_w.value);
            if (strcmp(scale_res_h.value, "source"))
                downscaleHeight = atoi(scale_res_h.value);
        }
        else if (!strcmp(scale_enable.value, "multiple")) {
            RETRO_VARIABLE(scale_mult_w);
            RETRO_VARIABLE(scale_mult_h);
            downscaleWidth = (float)newSourceWidth * atof(scale_mult_w.value);
            downscaleHeight = (float)newSourceHeight * atof(scale_mult_h.value);
        }

        if (newSourceWidth != croppedStoredDesc.Width ||
            newSourceHeight != croppedStoredDesc.Height ||
            newTextureDesc.Format != croppedStoredDesc.Format ||
            !convertReady)
        {
            // Recreate our cropping texture (stores cropped version of capture).
            // The GPU needs to sample this one, so we need an SRV over it as well.
            croppedStoredDesc = {};
            croppedStoredDesc.Width = newSourceWidth;
            croppedStoredDesc.Height = newSourceHeight;
            croppedStoredDesc.MipLevels = 1;
            croppedStoredDesc.ArraySize = 1;
            croppedStoredDesc.Format = newTextureDesc.Format;
            croppedStoredDesc.SampleDesc.Count = 1;
            croppedStoredDesc.Usage = D3D11_USAGE_DEFAULT;
            croppedStoredDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;

            this->croppedTexture = nullptr;
            this->device->CreateTexture2D(&croppedStoredDesc, nullptr, this->croppedTexture.put());

            this->croppedView = nullptr;
            this->device->CreateShaderResourceView(this->croppedTexture.get(), nullptr, this->croppedView.put());
        }

        if (downscaleWidth != convertedStoredDesc.Width ||
            downscaleHeight != convertedStoredDesc.Height ||
            !convertReady)
        {
            // Recreate our converted texture (stores converted format version of cropped texture above).
            // The GPU needs to render to this one, so we need it as a render target and viewport.
            convertedStoredDesc = croppedStoredDesc;
            convertedStoredDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
            convertedStoredDesc.BindFlags = D3D11_BIND_RENDER_TARGET;
            convertedStoredDesc.Width = downscaleWidth;
            convertedStoredDesc.Height = downscaleHeight;
            // Shared handles are needed for access to the texture from Vulkan.
            convertedStoredDesc.MiscFlags = D3D11_RESOURCE_MISC_SHARED_NTHANDLE | D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX;

            this->convertedTexture = nullptr;
            this->device->CreateTexture2D(&convertedStoredDesc, nullptr, this->convertedTexture.put());
            this->convertedTexture->QueryInterface(__uuidof(IDXGIKeyedMutex), (void**)this->convertedMutex.put());

            this->convertedTarget = nullptr;
            this->device->CreateRenderTargetView(this->convertedTexture.get(), nullptr, this->convertedTarget.put());

            this->convertedViewport = {};
            this->convertedViewport.Width = (FLOAT) convertedStoredDesc.Width;
            this->convertedViewport.Height = (FLOAT) convertedStoredDesc.Height;

            // Recreate our readback texture (identical to the above, but configured for CPU readback).
            D3D11_TEXTURE2D_DESC t2d;
            t2d = convertedStoredDesc;
            t2d.Usage = D3D11_USAGE_STAGING;
            t2d.BindFlags = 0;
            t2d.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
            t2d.MiscFlags = 0;

            this->readbackTexture = nullptr;
            this->device->CreateTexture2D(&t2d, nullptr, this->readbackTexture.put());
        }

        // First, we copy a region from the WGC texture to the cropping texture, which implements the crop.
        this->deviceContext->CopySubresourceRegion(this->croppedTexture.get(), 0, 0, 0, 0, newSourceTexture, 0, &sourceBox);
        convertReady = true;

        // Setup topmost hack at this point, now we have a fully working window.
        if (topmostedWindow == NULL)
            SetupTopmostHack();

        // Next we need to fire the GPU up for render-to-texture in order to convert formats.
        RunConvertPass();

        // Finally, we can copy from the converted texture to the one that's suitable for readback.
        // We only need to do this for software blit -- if we are using HW blit,
        //      the HW implementation will directly play with the converted texture.
        if (this->uploadMethod == "software")
        {
            convertedMutex->AcquireSync(0, INFINITE);
            this->deviceContext->CopyResource(this->readbackTexture.get(), this->convertedTexture.get());
            convertedMutex->ReleaseSync(0);
        }
    }

    

private:
    void SetupTopmostHack()
    {
        HWND retroarchWindow = FindWindow(L"RetroArch", NULL);
        // Couldn't find RetroArch, so let's just disable this functionality by returning here.
        if (!retroarchWindow) return;
        // Otherwise save the RetroArch window as the topmosted window.
        topmostedWindow = retroarchWindow;

        // Create a dummy window for handling a hotkey and register that hotkey (Ctrl-Alt-T).
        HINSTANCE instance = GetModuleHandle(NULL);
        WNDCLASS wc = {};
        wc.lpfnWndProc = DummyHotkeyWindowProc;
        wc.hInstance = instance;
        wc.lpszClassName = L"DummyHotkeyClass";
        RegisterClass(&wc);
        dummyHotkeyWindow = CreateWindow(wc.lpszClassName, L"Dummy Hotkey Window", 0,
            CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
            NULL, NULL, instance, NULL);
        SetWindowLongPtr(dummyHotkeyWindow, GWLP_USERDATA, (LONG_PTR)this);
        RegisterHotKey(dummyHotkeyWindow, TOPMOST_HOTKEY_ID, MOD_CONTROL | MOD_ALT, 'T');

        // Turn on topmost mode right away.
        // ToggleTopmostHack();
    }

    void CleanupTopmostHack()
    {
        // Destroy the dummy window we created.
        if(IsWindow(dummyHotkeyWindow))
            DestroyWindow(dummyHotkeyWindow);
    }

    void ToggleTopmostHack() {
        // If we don't have a target for topmosting, stop here.
        if (topmostedWindow == NULL)
            return;
        
        DWORD dwExStyle = GetWindowLong(topmostedWindow, GWL_EXSTYLE);
        const DWORD tmFlags = WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_APPWINDOW;
        if (!topmostMode) {
            SetWindowLongPtr(topmostedWindow, GWL_EXSTYLE, dwExStyle | tmFlags);
            SetWindowPos(topmostedWindow, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
            ShowCursor(false);
            SetForegroundWindow(captureWindow);
            topmostMode = true;
        } else {
            SetWindowLongPtr(topmostedWindow, GWL_EXSTYLE, dwExStyle & ~tmFlags);
            SetWindowPos(topmostedWindow, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE |  SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
            ShowCursor(true);
            SetForegroundWindow(topmostedWindow);
            topmostMode = false;
        }
    }

    static LRESULT CALLBACK DummyHotkeyWindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
    {
        CaptureState* captureState = (CaptureState*)GetWindowLongPtr(hwnd, GWLP_USERDATA);
        if (msg == WM_HOTKEY && wParam == TOPMOST_HOTKEY_ID)
            captureState->ToggleTopmostHack();

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    static BOOL CALLBACK EnumWindowProc(HWND testHwnd, LPARAM lParam)
    {
        SCORED_WINDOW* scoredWindow = (SCORED_WINDOW*)lParam;

        // Get the process that owns the window.
        DWORD windowProcessID;
        GetWindowThreadProcessId(testHwnd, &windowProcessID);

        // Get information about our window (title and classname).
        wchar_t className[1024], windowTitle[1024];
        GetClassName(testHwnd, className, 1024);
        GetWindowText(testHwnd, windowTitle, 1024);

        // Try to get further information about the program our window belongs to (name of the .exe for the owning process).
        wchar_t processFilepath[1024];
        HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, windowProcessID);
        GetModuleFileNameEx(hProcess, NULL, processFilepath, 1024);
        CloseHandle(hProcess);
        
        // And now we try to get yet more info about that process (product name / description from .exe).
        LPVOID processProductName = NULL, processFileDesc = NULL;
        UINT ppnLength, pfdLength;
        DWORD dummyArg;
        DWORD versionInfoSize = GetFileVersionInfoSizeEx(FILE_VER_GET_NEUTRAL, processFilepath, &dummyArg);
        BYTE* versionInfo = (BYTE*)malloc(versionInfoSize);
        GetFileVersionInfoEx(FILE_VER_GET_NEUTRAL, processFilepath, NULL, versionInfoSize, versionInfo);
        VerQueryValue(versionInfo, L"\\StringFileInfo\\040904B0\\ProductName", &processProductName, &ppnLength);
        VerQueryValue(versionInfo, L"\\StringFileInfo\\040904B0\\FileDescription", &processFileDesc, &pfdLength);
        if (processProductName == NULL)
            processProductName = (LPVOID)L"";
        if (processFileDesc == NULL)
            processFileDesc = (LPVOID)L"";

        // Fuzzy match on class and window names.
        double productNameScore = fuzz::partial_ratio((wchar_t*)processProductName, scoredWindow->partial) + 30;
        double titleScore       = fuzz::partial_ratio(windowTitle, scoredWindow->partial)                  + 20;
        double productDescScore = fuzz::partial_ratio((wchar_t*)processFileDesc, scoredWindow->partial)    + 10;
        double classScore       = fuzz::partial_ratio(className, scoredWindow->partial)                    + 0;
        double newScore = max(max(max(classScore, titleScore), productNameScore), productDescScore);

        // Greatly prefer visible windows.
        if (IsWindowVisible(testHwnd))
            newScore *= 2.0;
        // Slightly prefer longer titles -- helps find render windows rather than emulator base windows.
        newScore += wcslen(windowTitle);
        // Disprefer capturing OS windows (incl. Windows Explorer).
        if (wcsstr((wchar_t*)processProductName, L"Microsoft® Windows® Operating System"))
            newScore *= 0.5;
        // Disprefer capturing Microsoft Edge / Google Chrome / Electron Apps (per class name).
        if (wcsstr(className, L"Chrome_Widget"))
            newScore *= 0.5;

        if (newScore > scoredWindow->score)
        {
            scoredWindow->score = newScore;
            scoredWindow->window = testHwnd;
        }

        free(versionInfo);
        return TRUE;
    }

    void SetupConvertPass()
    {
        // Cool.. hardcoded shaders!
        std::string vsSource =
            "struct VSOutput { float4 position : SV_POSITION; float2 uv : TEXCOORD; };\n"
            "VSOutput main_vs(float4 position : POSITION, float2 uv : TEXCOORD) {\n"
            "    VSOutput output; output.position = float4(position.xy, 0.0f, 1.0f); output.uv = uv;\n"
            "    return output;"
            "}\n";
        std::string psSource =
            "struct VSOutput { float4 position : SV_POSITION; float2 uv : TEXCOORD; };\n"
            "texture2D croppedTexture : register(t0);\n"
            "SamplerState croppedSampler : register(s0);\n"
            "float4 main_ps(VSOutput input) : SV_TARGET {\n"
            "    float3 color = croppedTexture.Sample(croppedSampler, input.uv).xyz;\n"
            "    return float4(color.xyz, 1.0f);\n"
            "}\n";
        
        // If using software, the convert pass needs to perform a swizzle operation.
        if (this->uploadMethod == "software")
        {
            std::string from = "color.xyz", to = "color.zyx";
            psSource.replace(psSource.find(from), from.length(), to);
        }

        // Hardcoded vertex format..
        const D3D11_INPUT_ELEMENT_DESC ied[] =
        {
            {"POSITION", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0},
            {"TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 8, D3D11_INPUT_PER_VERTEX_DATA, 0},
        };

        // Compile our shaders and set up our render pass.
        this->convertPass = make_shared<Direct3DShaderPass>(this->device, this->deviceContext, vsSource.c_str(), psSource.c_str(), ied, 2);

        // Hardcoded vertices and indices!
        const VERTEX fqVertices[] =
        {
            {-1.0f, -1.0f, {0.0f, 1.0f}}, // bottom left  (0)
            { 1.0f, -1.0f, {1.0f, 1.0f}}, // bottom right (1)
            {-1.0f,  1.0f, {0.0f, 0.0f}}, // top left     (2)
            { 1.0f,  1.0f, {1.0f, 0.0f}}, // top right    (3)
        };
        const UINT fqIndices[] = { 0, 2, 3, 0, 3, 1 };

        // Create vertex buffer.
        D3D11_BUFFER_DESC bd = {};
        D3D11_SUBRESOURCE_DATA srd = {};
        bd.Usage = D3D11_USAGE_IMMUTABLE;
        bd.ByteWidth = sizeof(fqVertices);
        bd.BindFlags = D3D11_BIND_VERTEX_BUFFER;
        srd.pSysMem = fqVertices;

        this->device->CreateBuffer(&bd, &srd, this->fqVertexBuffer.put());

        // Create index buffer.
        bd.ByteWidth = sizeof(fqIndices);
        bd.BindFlags = D3D11_BIND_INDEX_BUFFER;
        srd.pSysMem = fqIndices;

        this->device->CreateBuffer(&bd, &srd, this->fqIndexBuffer.put());

        // Set up sampler states -- linear sampler.
        D3D11_SAMPLER_DESC sd = {};
        sd.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
        sd.AddressU = D3D11_TEXTURE_ADDRESS_CLAMP;
        sd.AddressV = D3D11_TEXTURE_ADDRESS_CLAMP;
        sd.AddressW = D3D11_TEXTURE_ADDRESS_CLAMP;
        sd.MinLOD = -FLT_MAX;
        sd.MaxLOD = FLT_MAX;
        sd.MaxAnisotropy = 1;

        this->device->CreateSamplerState(&sd, this->linearSampler.put());

        // And point sampler.
        sd.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;

        this->device->CreateSamplerState(&sd, this->pointSampler.put());
    }
    
    void RunConvertPass()
    {
        // Local temporaries for COM objects that must be passed as arrays.
        ID3D11Buffer* fqVertexBuffer = this->fqVertexBuffer.get();
        ID3D11SamplerState* linearSampler = this->linearSampler.get();
        ID3D11SamplerState* pointSampler = this->pointSampler.get();
        ID3D11ShaderResourceView* croppedView = this->croppedView.get();
        ID3D11RenderTargetView* convertedTarget = this->convertedTarget.get();

        // Stop if not yet ready (textures not yet created/filled).
        if (!convertReady)
            return;

        // Setup input assembler.
        UINT stride = sizeof(VERTEX);
        UINT offset = 0;
        this->deviceContext->IASetVertexBuffers(0, 1, &fqVertexBuffer, &stride, &offset);
        this->deviceContext->IASetIndexBuffer(this->fqIndexBuffer.get(), DXGI_FORMAT_R32_UINT, 0);
        this->deviceContext->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

        // Set up sampler.
        RETRO_VARIABLE(scale_filter_mode);
        if (!strcmp(scale_filter_mode.value, "linear")) {
            this->deviceContext->PSSetSamplers(0, 1, &linearSampler);
        }
        else {
            this->deviceContext->PSSetSamplers(0, 1, &pointSampler);
        }

        // Sync the keyed mutex on the converted texture.
        convertedMutex->AcquireSync(0, INFINITE);

        // Bind and run convert pass.
        this->convertPass->Activate();
        this->deviceContext->PSSetShaderResources(0, 1, &croppedView);
        this->deviceContext->RSSetViewports(1, &this->convertedViewport);
        this->deviceContext->OMSetRenderTargets(1, &convertedTarget, nullptr);
        this->deviceContext->DrawIndexed(6, 0, 0);
        this->deviceContext->OMSetRenderTargets(0, nullptr, nullptr);

        // Release the keyed mutex on the converted texture.
        convertedMutex->ReleaseSync(0);
    }
};

shared_ptr<CaptureState> captureState;

static void fallback_log(enum retro_log_level level, const char *fmt, ...)
{
   (void)level;
   va_list va;
   va_start(va, fmt);
   vfprintf(stderr, fmt, va);
   va_end(va);
}

void retro_init(void)
{
   const char *dir = NULL;
   if (environ_cb(RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY, &dir) && dir)
   {
      snprintf(retro_system_directory, sizeof(retro_system_directory), "%s", dir);
   }
   
}

void retro_deinit(void)
{

}

unsigned retro_api_version(void)
{
   return RETRO_API_VERSION;
}

void retro_set_controller_port_device(unsigned port, unsigned device)
{
   log_cb(RETRO_LOG_INFO, "Plugging device %u into port %u.\n", device, port);
}

void retro_get_system_info(struct retro_system_info *info)
{
   memset(info, 0, sizeof(*info));
   info->library_name     = "WindowCast";
   info->library_version  = "1.0";
   info->need_fullpath = false;
   info->valid_extensions = "txt";
}

void retro_get_system_av_info(struct retro_system_av_info *info)
{
   info->geometry.base_width   = NOMINAL_VIDEO_WIDTH;
   info->geometry.base_height  = NOMINAL_VIDEO_HEIGHT;
   info->geometry.max_width    = MAX_VIDEO_WIDTH;
   info->geometry.max_height   = MAX_VIDEO_HEIGHT;
   info->geometry.aspect_ratio = 0.0f;
}

// If these was a neater, cleaner way of specifying this other than just hardcoding these big tables here, and it *acutally worked*, I'd do it, believe me.
#define MULT_VALUES \
{ "1", "1x" }, { "2", "2x" }, { "3", "3x" }, { "4", "4x" }, { "5", "5x" }, { "6", "6x" }, \
{ "0.0625", "0.0625x (1/16)"},  { "0.125", "0.125x (1/8)"}, { "0.25", "0.25x (1/4)" }, { "0.334", "0.334x (1/3)"}, \
{ "0.5", "0.5x (1/2)" }, { "0.667", "0.667x (2/3)" }, { "0.75", "0.75x (3/4)" }, { NULL, NULL }
#define RESO_VALUES \
{ "128", NULL }, { "144", NULL }, { "160", NULL }, { "176", NULL }, { "192", NULL }, { "208", NULL }, { "224", NULL }, { "240", NULL }, \
{ "256", NULL }, { "272", NULL }, { "288", NULL }, { "304", NULL }, { "320", NULL }, { "336", NULL }, { "352", NULL }, { "368", NULL }, \
{ "384", NULL }, { "400", NULL }, { "416", NULL }, { "432", NULL }, { "448", NULL }, { "464", NULL }, { "480", NULL }, { "496", NULL }, \
{ "512", NULL }, { "528", NULL }, { "544", NULL }, { "560", NULL }, { "576", NULL }, { "592", NULL }, { "600", NULL }, { "608", NULL }, \
{ "624", NULL }, { "640", NULL }, { "656", NULL }, { "672", NULL }, { "688", NULL }, { "704", NULL }, { "720", NULL }, { "736", NULL }, \
{ "752", NULL }, { "768", NULL }, { "784", NULL }, { "800", NULL }, { "816", NULL }, { "832", NULL }, { "848", NULL }, { "864", NULL }, \
{ "880", NULL }, { "896", NULL }, { "900", NULL }, { "912", NULL }, { "928", NULL }, { "944", NULL }, { "960", NULL }, { "976", NULL }, \
{ "992", NULL }, { "1008", NULL }, { "1024", NULL }, { "1040", NULL }, { "1056", NULL }, { "1072", NULL }, { "1080", NULL }, { "1088", NULL }, \
{ "1104", NULL }, { "1120", NULL }, { "1136", NULL }, { "1152", NULL }, { "1168", NULL }, { "1184", NULL }, { "1200", NULL }, { "1216", NULL }, \
{ "1232", NULL }, { "1248", NULL }, { "1264", NULL }, { "1280", NULL }, { "1296", NULL }, { "1312", NULL }, { "1328", NULL }, { "1344", NULL }, \
{ "1360", NULL }, { "1376", NULL }, { "1392", NULL }, { "1408", NULL }, { "1424", NULL }, { "1440", NULL }, { "1456", NULL }, { "1472", NULL }, \
{ "1488", NULL }, { "1504", NULL }, { "1520", NULL }, { "1536", NULL }, { "1552", NULL }, { "1568", NULL }, { "1584", NULL }, { "1600", NULL }, \
{ "1616", NULL }, { "1632", NULL }, { "1648", NULL }, { "1664", NULL }, { "1680", NULL }, { "1696", NULL }, { "1712", NULL }, { "1728", NULL }, \
{ "1744", NULL }, { "1760", NULL }, { "1776", NULL }, { "1792", NULL }, { "1808", NULL }, { "1824", NULL }, { "1840", NULL }, { "1856", NULL }, \
{ "1872", NULL }, { "1888", NULL }, { "1904", NULL }, { "1920", NULL }, { "2160", NULL }, { "3840", NULL }, { "4096", NULL }, { "4320", NULL }, \
{ "7680", NULL }, { "8192", NULL }, { "16384", NULL }, { "source", "Don't scale this axis." }, { NULL, NULL }
#define ASPECT_VALUES \
{ "1.0", "Force 1:1" }, { "1.25", "Force 5:4" }, { "1.333", "Force 4:3" }, { "1.5", "Force 3:2" }, \
{ "1.6", "Force 16:10" }, { "1.778", "Force 16:9" }, { "1.85", "Force 1.85:1" }, { "2.333", "Force true 21:9" }, \
{ "2.39", "Force 2.39:1 ('21:9')" }, {"3.556", "Force 32:9"}, {"4.0", "Force 4:1; Vive la revolution!"}, { NULL, NULL }
#define FINE_VALUES \
{ "0", NULL }, { "1", NULL }, { "2", NULL }, { "3", NULL }, { "4", NULL }, { "5", NULL }, { "6", NULL }, { "7", NULL }, \
{ "8", NULL }, { "9", NULL }, { "10", NULL }, { "11", NULL }, { "12", NULL }, { "13", NULL }, { "14", NULL }, { "15", NULL }, \
{ "16", NULL }, { "17", NULL }, { "18", NULL }, { "19", NULL }, { "20", NULL }, { "21", NULL }, { "22", NULL }, { "23", NULL }, \
{ "24", NULL }, { "25", NULL }, { "26", NULL }, { "27", NULL }, { "28", NULL }, { "29", NULL }, { "30", NULL }, { "31", NULL }, \
{ "32", NULL }, { "33", NULL }, { "34", NULL }, { "35", NULL }, { "36", NULL }, { "37", NULL }, { "38", NULL }, { "39", NULL }, \
{ "40", NULL }, { "41", NULL }, { "42", NULL }, { "43", NULL }, { "44", NULL }, { "45", NULL }, { "46", NULL }, { "47", NULL }, \
{ "48", NULL }, { "49", NULL }, { "50", NULL }, { "51", NULL }, { "52", NULL }, { "53", NULL }, { "54", NULL }, { "55", NULL }, \
{ "56", NULL }, { "57", NULL }, { "58", NULL }, { "59", NULL }, { "60", NULL }, { "61", NULL }, { "62", NULL }, { "63", NULL }, \
{ "64", NULL }, { "65", NULL }, { "66", NULL }, { "67", NULL }, { "68", NULL }, { "69", NULL }, { "70", NULL }, { "71", NULL }, \
{ "72", NULL }, { "73", NULL }, { "74", NULL }, { "75", NULL }, { "76", NULL }, { "77", NULL }, { "78", NULL }, { "79", NULL }, \
{ "80", NULL }, { "81", NULL }, { "82", NULL }, { "83", NULL }, { "84", NULL }, { "85", NULL }, { "86", NULL }, { "87", NULL }, \
{ "88", NULL }, { "89", NULL }, { "90", NULL }, { "91", NULL }, { "92", NULL }, { "93", NULL }, { "94", NULL }, { "95", NULL }, \
{ "96", NULL }, { "97", NULL }, { "98", NULL }, { "99", NULL }
#define COARSE_VALUES \
{ "0", NULL }, { "100", NULL }, { "200", NULL }, { "300", NULL }, { "400", NULL }, { "500", NULL }, { "600", NULL }, { "700", NULL }, \
{ "800", NULL }, { "900", NULL }, { "1000", NULL }, { "1100", NULL }, { "1200", NULL }, { "1300", NULL }, { "1400", NULL }, { "1500", NULL }, \
{ "1600", NULL }, { "1700", NULL }, { "1800", NULL }, { "1900", NULL }, { "2000", NULL }, { "2100", NULL }, { "2200", NULL }, { "2300", NULL }, \
{ "2400", NULL }, { "2500", NULL }, { "2600", NULL }, { "2700", NULL }, { "2800", NULL }, { "2900", NULL }, { "3000", NULL }, { "3100", NULL }, \
{ "3200", NULL }, { "3300", NULL }, { "3400", NULL }, { "3500", NULL }, { "3600", NULL }, { "3700", NULL }, { "3800", NULL }, { "3900", NULL }, \
{ "4000", NULL }, { "4100", NULL }, { "4200", NULL }, { "4300", NULL }, { "4400", NULL }, { "4500", NULL }, { "4600", NULL }, { "4700", NULL }, \
{ "4800", NULL }, { "4900", NULL }, { "5000", NULL }, { "5100", NULL }, { "5200", NULL }, { "5300", NULL }, { "5400", NULL }, { "5500", NULL }, \
{ "5600", NULL }, { "5700", NULL }, { "5800", NULL }, { "5900", NULL }, { "6000", NULL }, { "6100", NULL }, { "6200", NULL }, { "6300", NULL }, \
{ "6400", NULL }, { "6500", NULL }, { "6600", NULL }, { "6700", NULL }, { "6800", NULL }, { "6900", NULL }, { "7000", NULL }, { "7100", NULL }, \
{ "7200", NULL }, { "7300", NULL }, { "7400", NULL }, { "7500", NULL }, { "7600", NULL }, { "7700", NULL }, { "7800", NULL }, { "7900", NULL }, \
{ "8000", NULL }, { "8100", NULL }, { "8200", NULL }, { "8300", NULL }, { "8400", NULL }, { "8500", NULL }, { "8600", NULL }, { "8700", NULL }, \
{ "8800", NULL }, { "8900", NULL }, { "9000", NULL }, { "9100", NULL }, { "9200", NULL }, { "9300", NULL }, { "9400", NULL }, { "9500", NULL }, \
{ "9600", NULL }, { "9700", NULL }, { "9800", NULL }, { "9900", NULL }

void retro_set_environment(retro_environment_t cb)
{
   // Setup environment callback.
   environ_cb = cb;

   // Setup logging callback.
   if (cb(RETRO_ENVIRONMENT_GET_LOG_INTERFACE, &logging))
      log_cb = logging.log;
   else
      log_cb = fallback_log;

   // Setup core options.
   static const retro_core_option_definition coreOptions[] = {
       // Blit options.
       { "upload_method", "Upload method",
            "How to upload captured window data back to LibRetro.",
            {{"vulkan", "Upload using Vulkan interop. Fast, requires Vulkan renderer in frontend."},
             {"software", "Upload using CPU upload. Slow, but compatible."}, { NULL, NULL }}, "vulkan"},
       // Scaling options.
       { "scale_enable", "Enable downscaling/upscaling",
            "Enables downscaling/upscaling captured frames to a different internal resolution.",
            {{"disabled", "Disable scaling"}, {"exact", "Scale to an exact resolution (specified below)"},
             {"multiple", "Scale to a multiple of the captured window's resolution (specified below)"}, { NULL, NULL }}, "disabled"},
       { "scale_res_w", "Exact width for scaling",
            "Scale captured frames to this width if exact scaling is enabled above.", { RESO_VALUES }, "1280" },
       { "scale_res_h", "Exact height for scaling",
            "Scale captured frames to this height if exact scaling is enabled above.", { RESO_VALUES }, "720" },
       { "scale_mult_w", "Width multiple for scaling",
            "Scale captured frame width by this multiplier if multiple scaling is enabled above.", { MULT_VALUES }, "2"},
       { "scale_mult_h", "Height multiple for scaling",
            "Scale captured frame height by this multiplier if multiple scaling is enabled above.", { MULT_VALUES }, "2" },
       { "scale_filter_mode", "Filtering mode for scaling",
            "Changes texture filter used when downscaling/upscaling",
            {{"linear", "Scale using linear filtering"}, {"point", "Scale using neighest-neighbor (no filtering)"}, { NULL, NULL }}, "linear"},
       // Aspect ratio options.
       { "aspect_ratio_mode", "Aspect ratio mode",
            "Choose what method is used to report core aspect ratio to libretro. This value is used if \"Core Provided\" is set in RA's Settings->Video->Scaling.",
            {{"cropped", "Use the aspect ratio of the cropped frame, pre-scaling (default)."},
             {"scaled", "Use the aspect ratio of the scaled frame."}, ASPECT_VALUES }, "cropped" },
       // Cropping options.
       { "crop_enable", "Crop window titlebar/borders",
            "If enabled, window titlebars & borders will not be captured. If disabled, the entire window will be captured. Enabled by default.",
            {{"true", "Enabled"}, {"false", "Disabled"}, { NULL, NULL }}, "true" },
       { "client_crop_mode", "Client-area cropping mode",
            "How to further crop captured client area. This option has no effect if window titlebars & borders are not cropped.",
            {{"auto", "Default (try to guess)"}, {"disabled", "Disabled (don't crop)"}, {"custom", "Custom (use parameters below)"}, { NULL, NULL }}, "auto" },
       { "client_crop_left_fine", "Client-area crop: left (fine-adjust)",
            "Crop the left of the client area by this many pixels.", { FINE_VALUES }, "0"},
       { "client_crop_right_fine", "Client-area crop: right (fine-adjust)",
            "Crop the right of the client area by this many pixels.", { FINE_VALUES }, "0"},
       { "client_crop_top_fine", "Client-area crop: top (fine-adjust)",
            "Crop the top of the client area by this many pixels.", { FINE_VALUES }, "0"},
       { "client_crop_bottom_fine", "Client-area crop: bottom (fine-adjust)",
            "Crop the bottom of the client area by this many pixels.", { FINE_VALUES }, "0"},
       { "client_crop_left_coarse", "Client-area crop: left (coarse-adjust)",
            "Crop the left of the client area by this many pixels.", { COARSE_VALUES }, "0" },
       { "client_crop_right_coarse", "Client-area crop: right (coarse-adjust)",
            "Crop the right of the client area by this many pixels.", { COARSE_VALUES }, "0"},
       { "client_crop_top_coarse", "Client-area crop: top (coarse-adjust)",
            "Crop the top of the client area by this many pixels.", { COARSE_VALUES }, "0"},
       { "client_crop_bottom_coarse", "Client-area crop: bottom (coarse-adjust)",
            "Crop the bottom of the client area by this many pixels.", { COARSE_VALUES }, "0"},
       { NULL, NULL, NULL, {{0}}, NULL }
   };
   cb(RETRO_ENVIRONMENT_SET_CORE_OPTIONS, (void*) &coreOptions);
}

void retro_set_audio_sample(retro_audio_sample_t cb)
{
   audio_cb = cb;
}

void retro_set_audio_sample_batch(retro_audio_sample_batch_t cb)
{
   audio_batch_cb = cb;
}

void retro_set_input_poll(retro_input_poll_t cb)
{
   input_poll_cb = cb;
}

void retro_set_input_state(retro_input_state_t cb)
{
   input_state_cb = cb;
}

void retro_set_video_refresh(retro_video_refresh_t cb)
{
   video_cb = cb;
}

void retro_reset(void)
{
    // Select which window to capture and set up capture context.
    if (!captureState->SetupCaptureContext()) {
        log_cb(RETRO_LOG_ERROR, "Can't find window to capture.\n");
        environ_cb(RETRO_ENVIRONMENT_SHUTDOWN, NULL);
        return;
    }

    // Reset our frame counter.
    frame_counter = 0;
}

void retro_run(void)
{
    // Check for capture, and present if we have it.
    WGCCapture::CaptureReadiness captureReady = WGCCapture::CaptureReadiness::CAPTURE_DEAD;
    if (captureState->capture)
        captureReady = captureState->capture->IsCaptureReady();

    // Now if the capture is ready, let's do it.
    if (captureReady == WGCCapture::CaptureReadiness::CAPTURE_READY)
    {
        // Copy the latest capture for readback.
        captureState->UpdateCaptureTextures();

        // Get information about the latest capture in the *converted* texture.
        // We use the converted texture because it's properties are common between itself and the readback texture,
        //      and Vulkan will use the converted texture directly, meaning that we might not even *have* a valid readback texture.
        D3D11_TEXTURE2D_DESC convertedDesc;
        captureState->convertedTexture.get()->GetDesc(&convertedDesc);

        // Calculate new aspect ratio / geometry and inform libretro about changes to such if needed.
        retro_game_geometry newGeometry;
        newGeometry.base_width = convertedDesc.Width;
        newGeometry.base_height = convertedDesc.Height;
        RETRO_VARIABLE(aspect_ratio_mode);
        if (!strcmp(aspect_ratio_mode.value, "scaled")) {
            newGeometry.aspect_ratio = (float)convertedDesc.Width / (float)convertedDesc.Height;
        }
        else if (!strcmp(aspect_ratio_mode.value, "cropped")) {
            newGeometry.aspect_ratio = (float)captureState->croppedStoredDesc.Width / (float)captureState->croppedStoredDesc.Height;
        }
        else { // Otherwise, we're forcing a specific aspect ratio value.
            newGeometry.aspect_ratio = atof(aspect_ratio_mode.value);
        }
        if (game_geom_saved.base_width != newGeometry.base_width ||
            game_geom_saved.base_height != newGeometry.base_height ||
            game_geom_saved.aspect_ratio != newGeometry.aspect_ratio) {
            environ_cb(RETRO_ENVIRONMENT_SET_GEOMETRY, &newGeometry);
            game_geom_saved = newGeometry;
        }

        if (captureState->uploadMethod == "vulkan")
        {
            // Just ask Vulkan to handle the blit.
            vkState->BlitD3D11Texture(captureState->device.get(), captureState->convertedTexture.get());
        }
        else if (captureState->uploadMethod == "software")
        {
            // Now bind the readback texture for read.
            D3D11_MAPPED_SUBRESOURCE ms;
            captureState->deviceContext->Map(captureState->readbackTexture.get(), 0, D3D11_MAP_READ, 0, &ms);
            const uint8_t* readPointer = (uint8_t*)ms.pData;

            // Time to blit to LibRetro!
            retro_framebuffer frameBufferInfo = {};
            frameBufferInfo.width = convertedDesc.Width;
            frameBufferInfo.height = convertedDesc.Height;
            frameBufferInfo.access_flags = RETRO_MEMORY_ACCESS_WRITE;
            environ_cb(RETRO_ENVIRONMENT_GET_CURRENT_SOFTWARE_FRAMEBUFFER, &frameBufferInfo);
            uint8_t* writePointer = (uint8_t*)frameBufferInfo.data;

            for (unsigned i = 0; i < convertedDesc.Height; i++) {
                memcpy(writePointer, readPointer, convertedDesc.Width * 4);
                readPointer += ms.RowPitch;
                writePointer += frameBufferInfo.pitch;
            }

            video_cb(frameBufferInfo.data, frameBufferInfo.width, frameBufferInfo.height, frameBufferInfo.pitch);

            // Unmap the readback texture.
            captureState->deviceContext->Unmap(captureState->readbackTexture.get(), 0);
        }
    }
    // Otherwise, we need to set up the capture context again.
    else if (captureReady == WGCCapture::CaptureReadiness::CAPTURE_DEAD)
    {
        retro_reset();
    }

    // Increment the master frame counter.
    frame_counter++;
}

VkInstance vulkan_create_instance(
    PFN_vkGetInstanceProcAddr get_instance_proc_addr,
    const VkApplicationInfo* app,
    retro_vulkan_create_instance_wrapper_t create_instance_wrapper,
    void* opaque)
{
    VkInstanceCreateInfo createInfo = {VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO};
    const char* layers[] = { "VK_LAYER_KHRONOS_validation" };
    createInfo.pApplicationInfo = app;
    //createInfo.enabledLayerCount = sizeof(layers)/sizeof(layers[0]);
    //createInfo.ppEnabledLayerNames = layers;
    return create_instance_wrapper(opaque, &createInfo);
}

bool vulkan_create_device(
    struct retro_vulkan_context* context,
    VkInstance instance,
    VkPhysicalDevice gpu,
    VkSurfaceKHR surface,
    PFN_vkGetInstanceProcAddr get_instance_proc_addr,
    retro_vulkan_create_device_wrapper_t create_device_wrapper,
    void* opaque)
{
    // Setup queue families and queue creation info.
    vulkan_symbol_wrapper_init(get_instance_proc_addr);
    vulkan_symbol_wrapper_load_core_instance_symbols(instance);
    vulkan_symbol_wrapper_load_instance_symbol(instance, "vkGetPhysicalDeviceSurfaceSupportKHR", (PFN_vkVoidFunction*)&vkGetPhysicalDeviceSurfaceSupportKHR);

    uint32_t queueFamilyCount = 0;
    vkGetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, nullptr);
    std::vector<VkQueueFamilyProperties> queueFamilies(queueFamilyCount);
    vkGetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, queueFamilies.data());

    std::optional<uint32_t> mainQFIdx, presentQFIdx;
    for (unsigned i = 0; i < queueFamilies.size(); i++) {
        if (queueFamilies[i].queueFlags & VK_QUEUE_GRAPHICS_BIT && queueFamilies[i].queueFlags & VK_QUEUE_COMPUTE_BIT)
            mainQFIdx = i;

        VkBool32 supportsPresent = FALSE;
        vkGetPhysicalDeviceSurfaceSupportKHR(gpu, i, surface, &supportsPresent);
        if (supportsPresent)
            presentQFIdx = i;

        if (mainQFIdx.has_value() && presentQFIdx.has_value())
            break;
    }

    std::vector<VkDeviceQueueCreateInfo> queueCreateInfos;
    std::set<uint32_t> uniqueQueueFamilies = { mainQFIdx.value(), presentQFIdx.value() };
    float queuePriority = 1.0f;
    for (uint32_t queueFamily : uniqueQueueFamilies) {
        VkDeviceQueueCreateInfo queueCreateInfo = { VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO };
        queueCreateInfo.queueFamilyIndex = queueFamily;
        queueCreateInfo.queueCount = 1;
        queueCreateInfo.pQueuePriorities = &queuePriority;
        queueCreateInfos.push_back(queueCreateInfo);
    }

    // Check supported extensions.
    uint32_t deviceExtensionCount;
    vkEnumerateDeviceExtensionProperties(gpu, NULL, &deviceExtensionCount, NULL);
    std::vector<VkExtensionProperties> deviceExtensionProps(deviceExtensionCount);
    vkEnumerateDeviceExtensionProperties(gpu, NULL, &deviceExtensionCount, deviceExtensionProps.data());

    const char* ourExtensions[] = { VK_KHR_EXTERNAL_MEMORY_WIN32_EXTENSION_NAME };
    unsigned ourExtensionCount = sizeof(ourExtensions) / sizeof(ourExtensions[0]);
    for (unsigned i = 0; i < ourExtensionCount; i++)
    {
        const char* ourExtension = ourExtensions[i];
        bool extensionFound = false;

        for (auto deviceExtension : deviceExtensionProps)
        {
            if (!strcmp(deviceExtension.extensionName,ourExtension)) {
                extensionFound = true; break;
            }
        }

        if (!extensionFound) {
            log_cb(RETRO_LOG_ERROR, "Vulkan upload method requires support for the %s extension.", ourExtension);
            environ_cb(RETRO_ENVIRONMENT_SHUTDOWN, NULL);
            return false;
        }
    }

    // Setup device and get queues.
    VkPhysicalDeviceFeatures pdf = { 0 };
    VkDeviceCreateInfo createInfo = { VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO };
    VkQueue mainQueue, presentQueue;
    createInfo.queueCreateInfoCount = static_cast<uint32_t>(queueCreateInfos.size());
    createInfo.pQueueCreateInfos = queueCreateInfos.data();
    createInfo.pEnabledFeatures = &pdf;
    createInfo.enabledExtensionCount = ourExtensionCount;
    createInfo.ppEnabledExtensionNames = ourExtensions;
    VkDevice device = create_device_wrapper(gpu, opaque, &createInfo);
    
    vulkan_symbol_wrapper_load_core_device_symbols(device);
    vkGetDeviceQueue(device, mainQFIdx.value(), 0, &mainQueue);
    vkGetDeviceQueue(device, presentQFIdx.value(), 0, &presentQueue);

    // dummied testing code -- which has already revealed a need to use VkMemoryDedicatedRequirements/VkMemoryDedicatedAllocateInfo
    /*PFN_vkGetPhysicalDeviceImageFormatProperties2 vkGetPhysicalDeviceImageFormatProperties2;
    vulkan_symbol_wrapper_load_instance_symbol(instance, "vkGetPhysicalDeviceImageFormatProperties2", (PFN_vkVoidFunction*)&vkGetPhysicalDeviceImageFormatProperties2);
    VkExternalImageFormatProperties extImageProps = { VK_STRUCTURE_TYPE_EXTERNAL_IMAGE_FORMAT_PROPERTIES };
    VkImageFormatProperties2 formatProps = { VK_STRUCTURE_TYPE_IMAGE_FORMAT_PROPERTIES_2 };
    formatProps.pNext = &extImageProps;

    VkPhysicalDeviceExternalImageFormatInfo  extImageInfo = { VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_EXTERNAL_IMAGE_FORMAT_INFO };
    extImageInfo.handleType = VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT;
    VkPhysicalDeviceImageFormatInfo2 formatInfo = { VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_IMAGE_FORMAT_INFO_2 };
    formatInfo.pNext = &extImageInfo;
    formatInfo.type = VK_IMAGE_TYPE_2D;
    formatInfo.flags |= VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT;
    formatInfo.format = VK_FORMAT_R8G8B8A8_UNORM;
    formatInfo.tiling = VK_IMAGE_TILING_OPTIMAL;
    formatInfo.usage = 
        VK_IMAGE_USAGE_SAMPLED_BIT |
        VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
        VK_IMAGE_USAGE_TRANSFER_DST_BIT;
    vkGetPhysicalDeviceImageFormatProperties2(gpu, &formatInfo, &formatProps);*/

    // Return context to libretro.
    context->gpu = gpu;
    context->device = device;
    context->queue = mainQueue;
    context->queue_family_index = mainQFIdx.value();
    context->presentation_queue = presentQueue;
    context->presentation_queue_family_index = presentQFIdx.value();
    vulkan_mainqf_idx = mainQFIdx.value();
    vulkan_presentqf_idx = presentQFIdx.value();
    return true;
}

static void context_reset_vulkan(void)
{
    // Get Vulkan rendering interface.
    if (!environ_cb(RETRO_ENVIRONMENT_GET_HW_RENDER_INTERFACE, (void**)&vulkan) || !vulkan)
    {
        log_cb(RETRO_LOG_ERROR, "Failed to get Vulkan rendering interface!\n");
        return;
    }

    // Check if HW render interface version is correct.
    if (vulkan->interface_version != RETRO_HW_RENDER_INTERFACE_VULKAN_VERSION)
    {
        fprintf(stderr, "Vulkan render interface mismatch, expected %u, got %u!\n",
            RETRO_HW_RENDER_INTERFACE_VULKAN_VERSION, vulkan->interface_version);
        vulkan = NULL;
        return;
    }

    // Reload Vulkan symbols now we have a stable device.
    vulkan_symbol_wrapper_init(vulkan->get_instance_proc_addr);
    vulkan_symbol_wrapper_load_core_instance_symbols(vulkan->instance);
    vulkan_symbol_wrapper_load_core_device_symbols(vulkan->device);

    // Start setting up Vulkan rendering structures.
    // The constructor of VulkanRenderState will do the bulk of our Vulkan init.
    vkState = make_shared<VulkanRenderState>();
}

void context_destroy_vulkan(void)
{
    vkState.reset();
    vulkan = nullptr;
}

const VkApplicationInfo* vulkan_get_app_info(void)
{
    static VkApplicationInfo info = {
        VK_STRUCTURE_TYPE_APPLICATION_INFO,
        NULL,
        "WindowCast for LibRetro",
        0, NULL, 0,
        VULKAN_API_VERSION
    };
    return &info;
}

bool retro_load_game(const struct retro_game_info *info)
{
    // Load capture partials from provided text file.
    if (info->size == 0) {
        log_cb(RETRO_LOG_ERROR, "No data in capture partials input file.\n");
        return false;
    }
    std::stringstream ss((char*)info->data);
    std::string line;
    std::vector<std::wstring> capturePartials;
    wchar_t wBuffer[4096];
    while (std::getline(ss, line, '\n')) {
        const char* whitespaces = " \n\t\r\f\v";
        if(line.find_first_of('#') != std::string::npos)
            line.erase(line.find_first_of('#')); // Comment support.
        line.erase(0, line.find_first_not_of(whitespaces)); // Strip leading whitespace.
        line.erase(line.find_last_not_of(whitespaces) + 1); // Strip trailing whitespace.
        if (line.empty())
            continue;

        log_cb(RETRO_LOG_INFO, "Capture partial read: %s\n", line.c_str());

        mbstowcs_s(nullptr, wBuffer, line.c_str(), _TRUNCATE);
        capturePartials.push_back(std::wstring(wBuffer));
    }

    // Set pixel buffer format. 
    enum retro_pixel_format fmt = RETRO_PIXEL_FORMAT_XRGB8888;
    if (!environ_cb(RETRO_ENVIRONMENT_SET_PIXEL_FORMAT, &fmt))
    {
        log_cb(RETRO_LOG_ERROR, "XRGB8888 is not supported.\n");
        return false;
    }

    // Check we have the correct H/W driver for selected upload method.
    RETRO_VARIABLE(upload_method);
    enum retro_hw_context_type preferredHW;
    environ_cb(RETRO_ENVIRONMENT_GET_PREFERRED_HW_RENDER, &preferredHW);
    if (!strcmp(upload_method.value, "vulkan") && preferredHW != RETRO_HW_CONTEXT_VULKAN)
    {
        log_cb(RETRO_LOG_WARN, "Vulkan upload requires Vulkan RetroArch driver. Falling back to software.\n");
        upload_method.value = "software";
    }

    // Set up H/W rendering context.
    if (!strcmp(upload_method.value, "vulkan"))
    {
        hw_render.context_type = RETRO_HW_CONTEXT_VULKAN;
        hw_render.version_major = VULKAN_API_VERSION;
        hw_render.version_minor = 0;
        hw_render.context_reset = context_reset_vulkan;
        hw_render.context_destroy = context_destroy_vulkan;
        hw_render.cache_context = true;
        if (!environ_cb(RETRO_ENVIRONMENT_SET_HW_RENDER, &hw_render))
            return false;

        static retro_hw_render_context_negotiation_interface_vulkan iface;
        iface.interface_type = RETRO_HW_RENDER_CONTEXT_NEGOTIATION_INTERFACE_VULKAN;
        iface.interface_version = RETRO_HW_RENDER_CONTEXT_NEGOTIATION_INTERFACE_VULKAN_VERSION;
        iface.get_application_info = vulkan_get_app_info;
        iface.create_instance = vulkan_create_instance;
        iface.create_device = (retro_vulkan_create_device_t)0x42069; // Pontential bug in RA 1.15.0, it won't call CD2 if we don't have a non-zero CD address.
        iface.create_device2 = vulkan_create_device;
        environ_cb(RETRO_ENVIRONMENT_SET_HW_RENDER_CONTEXT_NEGOTIATION_INTERFACE, (void*)&iface);
    }

    // Create capture state.
    captureState = make_shared<CaptureState>(capturePartials, upload_method.value);

    (void)info;
    return true;
}

void retro_unload_game(void)
{
    // Destroy capture state. Destructor stack should clean up everything else.
    captureState.reset();
}

unsigned retro_get_region(void)
{
   return RETRO_REGION_NTSC;
}

bool retro_load_game_special(unsigned type, const struct retro_game_info *info, size_t num)
{
   return false;
}

size_t retro_serialize_size(void)
{
   return 0;
}

bool retro_serialize(void *data_, size_t size)
{
   return false;
}

bool retro_unserialize(const void *data_, size_t size)
{
   return false;
}

void *retro_get_memory_data(unsigned id)
{
   (void)id;
   return NULL;
}

size_t retro_get_memory_size(unsigned id)
{
   (void)id;
   return 0;
}

void retro_cheat_reset(void)
{}

void retro_cheat_set(unsigned index, bool enabled, const char *code)
{
   (void)index;
   (void)enabled;
   (void)code;
}

