using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using WIGU;

namespace WIGUx.Modules.HomeAssistant
{
    public enum HomeAssistantDomains
    {
        light
    }

    public interface IHomeAssistantService
    {
        void SetLightToggle(string identifier);
        string SetLightState(string identifier, bool enabled);
    }

    public class HomeAssistantApi : IHomeAssistantService
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        string url = "http://homeassistant.local:8123";
        string api = "/api";
        string state = "/states";
        string services = "/services";

        string token;
       
        public HomeAssistantApi SetToken(string token)
        {
            this.token = token;
            return this;
        }

        public async Task<LightState> GetLightStateAsync(string identifier)
        {
            var path = Path.Combine(url, api, state, identifier);
            var data = await FetchDataFromApi(path, token);
            LightState lightState = JsonConvert.DeserializeObject<LightState>(data);
            return lightState;
        }

        public void SetLightToggle(string identifier)
        {
            logger.Info($"SetLightToggle!!!!");
            // services / light/turn_on
            var path =   $"{url}{api}{services}/{HomeAssistantDomains.light}/toggle";
            var dadta = new
            {
                entity_id = identifier
            };
            PostData(path, token, dadta);
        }

        public string SetLightState(string identifier, bool enabled)
        {
            // services / light/turn_on
            var path = Path.Combine(url, api, services, HomeAssistantDomains.light.ToString(), enabled ? "turn_on" : "turn_off");

            var dadta = new
            {
                entity_id = identifier
            };
            return PostData(path, token, dadta);
        }

        static string PostData(string url, string token, object data)
        {
            string jsonData = JsonConvert.SerializeObject(data);

            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "Bearer " + token;

            // Escribir el contenido JSON en el cuerpo de la solicitud
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(jsonData);
            }

            try
            {
                // Enviando la petición y obteniendo la respuesta
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (WebException e)
            {
                logger.Info($"WebException!!!!" + e.ToString());

                try
                {
                    // Manejo de errores de la red o códigos de estado HTTP de error
                    if (e.Response != null)
                    {
                        using (var errorResponse = (HttpWebResponse)e.Response)
                        using (var streamReader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            return $"Error: {errorResponse.StatusCode}, {streamReader.ReadToEnd()}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Info($"WebException222!!!!" + ex.ToString());
                }
               
                return $"Exception caught: {e.Message}";
            }
        }

        static async Task<string> FetchDataFromApi(string url, string bearerToken)
        {
            using (HttpClient client = new HttpClient())
            {
                // Configurando las cabeceras HTTP
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                try
                {
                    // Realizando la petición GET
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        // Leyendo la respuesta
                        string responseData = await response.Content.ReadAsStringAsync();
                        return responseData;
                    }
                    else
                    {
                        return $"Error: {response.StatusCode}";
                    }
                }
                catch (Exception e)
                {
                    return $"Exception caught: {e.Message}";
                }
            }
        }


        public class LightState
        {
            public string EntityId { get; set; }
            public string State { get; set; }
            public Attributes Attributes { get; set; }
            public DateTime LastChanged { get; set; }
            public DateTime LastReported { get; set; }
            public DateTime LastUpdated { get; set; }
            public Context Context { get; set; }
        }

        public class Attributes
        {
            public List<string> SupportedColorModes { get; set; }
            public string ColorMode { get; set; }
            public string FriendlyName { get; set; }
            public int SupportedFeatures { get; set; }
        }

        public class Context
        {
            public string Id { get; set; }
            public string ParentId { get; set; }
            public string UserId { get; set; }
        }

    }
}