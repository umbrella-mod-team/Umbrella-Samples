using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WIGU.Modules.Texture
{

    public class MultipleTextureLoaderController : MonoBehaviour
    {
        IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        Material Material;
        int index = 0;
        string[] textures = new string[0];

        void Start ()
        {
            logger.Info("TextureLoaderController> Start");

            var meshrenderer = GetComponent<MeshRenderer>();
            if (meshrenderer == null)
            {
                logger.Info("TextureLoaderController> Mesh Renderer is null");
                return;
            }

            var renderer = meshrenderer.GetComponent<Renderer>();
            if (renderer == null)
            {
                logger.Info("TextureLoaderController> Renderer is null TextureLoaderController");
                return;
            }

            logger.Info($"TextureLoaderController> Listing materials:");
            // first we associate 
            foreach (Material mat in renderer.materials)
            {
                logger.Info($"TextureLoaderController> {mat.name}");
                if (Material == null)
                {
                    if (mat.name.StartsWith("DefaultMaterial"))
                    {
                        Material = mat;
                    }
                }
            }

            logger.Info($"TextureLoaderController> MainCover exists {Material != null}");

            // read all the textures
            var name = gameObject.name.Substring("ugc_".Length).Replace("(Clone)", "");
            var path = Path.Combine(WIGU.EmuVrDirectoryPath, "Custom", "Multitexture", name);
            logger.Info($"TextureLoaderController> Path:  {path}");

            textures = Directory.GetFiles(path, "*.jpg");
            logger.Info("TextureLoaderController> " + path + " total:" + textures.Length);
            Print();

            StartCoroutine(ChangeTexture(textures[index]));
        }

        private void NextTexture()
        {
            if (++index >= textures.Length)
            {
                index = 0;
            }
            ChangeCurrentTexture();
        }

        private void ChangeCurrentTexture() => StartCoroutine(ChangeTexture(textures[index]));

        private IEnumerator ChangeTexture(string path)
        {
            var textureLoader = RemoteTextureManager.GetTextureLoader(path);
            while (!textureLoader.isDone)
                yield return (object)null;
            if (textureLoader.isError)
                yield break;

            Material.mainTexture = textureLoader.texture;
        } 

        private void Print()
        {
            logger.Info($"TextureLoaderController> Index {index}/{textures.Length}");
        }

        //public override void OnLoadJson(JsonObject json)
        //{
        //    logger.Info($"TextureLoaderController> OnLoadJson " + json.GetType().FullName + " " + json.id);

        //    MultipleTextureObject obj = (MultipleTextureObject)json;
        //    for (int i = 0; i < textures.Length; i++)
        //    {
        //        if (obj.Path == textures[index])
        //        {
        //            index = i;
        //            ChangeCurrentTexture();
        //            return;
        //        }
        //    }
        //}

        //public override void OnSaveJson(JsonObject json)
        //{
        //    logger.Info($"TextureLoaderController> OnLoadJson " + json.GetType().FullName + " " + json.id);
        //    ((MultipleTextureObject)json).Path = textures[index];
        //}

        void Update ()
        {
            // If user is not selecting or grabbing the object no need to continue
            if (!PlayerControllerHelper.IsObjectSelectedOrGrabbed(gameObject))
                return;

            if (Input.GetKeyDown(KeyCode.J))
            {
                NextTexture();
                Print();
            }
        }
    }
}
