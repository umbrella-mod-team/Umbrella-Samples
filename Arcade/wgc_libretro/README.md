# WindowCast for Libretro (formerly WGC Window Capture)

Libretro core to capture the contents of another window for video processing. This is useful, for say, capturing the output of a standalone emulator (like xemu, Dolphin standalone, RPCS3 or PCSX2 nightlies) or a PC game running in a window and then processing it with RetroArch's shader stack.

This core uses software blit, and should support running with any RetroArch video driver (vulkan, d3d11, gl, etc). However, the method used for window capture is Windows 10/11 specific and requires a Direct3D 10/11 capable GPU.

Audio or input is not handled, and it is expected that the game will be running in the background.

## System Requirements

- Windows 10, version 1803 or later (required as this uses the modern Windows Graphics Capture API).
- Fast modern GPU which supports Direct3D 10+. Vulkan blit (default) requires support for Vulkan 1.1 and the VK_KHR_external_memory_win32 extension. Make sure your GPU drivers are up-to-date.

## General Usage

Install core and info file into RetroArch's 'cores' and 'info' directories. You may need to set the RetroArch option 'core_info_cache_enable = "false"' for the core description to be properly detected.

Start RetroArch and 'Load Content', then load the 'partials-example.txt' file or another text file describing what windows the core will attempt to find and capture.

If the wrong window is detected at any point, use the 'Restart' option from RetroArch's quick menu. Options related to scaling, aspect ratio correction and cropping of the captured window can be found in the Core Options menu, and are documented there.

## Tips and Tricks

- The game or emulator should be running in a window, and emulators should be set to accept background input if possible. For visual results you should set the game resolution to a low resolution like 640x480 or 800x600, or use 1x native settings in your emulator. [Sizer](http://brianapps.net/sizer4/) can help with resizing windows to an exact resolution if you can't set the window resolution in the game/emulator settings.
- To enable playing PC games 'in the background' while RetroArch is running, this program includes a 'topmost hack' to make RetroArch appear above all other windows, while also ignoring all direct user input, and automatically focusing on the captured window (your emulator or game). Press 'CTRL-ALT-T' to activate this. If you need to change back to RetroArch, press 'CTRL-ALT-T' at any time to focus RetroArch again.
- Some users with Nvidia GPUs may experience issues with the capture image in RetroArch 'freezing' in fullscreen mode when certain applications are used. To help resolve this, open the Nvidia Control Panel. In the "Manage 3D Settings" tab, change the option "Vulkan/OpenGL present method" to "Prefer layered on DXGI Swapchain". If this option does not appear, you need to update your GPU drivers.
- Windows 11 users should use [Win11DisableRoundedCorners](https://github.com/valinet/Win11DisableRoundedCorners) to fix a visual bug that arises due to new DWM (Desktop Window Manager) behaviour.

## Partials File Format

The input format is simple. Each line in the file is treated as partial match to window titles and 'class names'. Comments (using '#') are supported.

## Technical Details

The presentation pipeline of this core using Direct3D11, however final copy to libretro is done via software blit, and the core creates it's own private D3D11 device (context). It should work for all frontend video drivers (vulkan, d3d11, d3d12, gl, etc). Presented frames are processed in 5 stages;

1. Use Windows Graphics Capture APIs to obtain a reference to a capture frame from the captured window, which is located on GPU.
2. Partial-copy this to a private GPU texture, performing any cropping via this copy.
3. Render a full-screen quad on GPU to copy this texture to another texture, to convert its format to what libretro expects. This stage also performs any downscaling/upscaling required.
4. Copy the converted format texture to a new texture that is marked usuable for CPU readback (this effects a VRAM->RAM copy).
5. Map this readback texture, and blit its pixels to libretro's software rendering buffers.