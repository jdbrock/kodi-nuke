using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SonarrSharp
{
    public class SonarrClient
    {
        public SonarrClientSeries Series { get; }

        private RestClient _client;

        public SonarrClient(string baseUri, string apiKey)
        {
            _client = new RestClient(baseUri);
            _client.AddDefaultHeader("X-Api-Key", apiKey);

            Series = new SonarrClientSeries(_client);
        }
    }

    public class SonarrClientSeries
    {
        private const string endpoint = "series";

        private RestClient _client;

        public SonarrClientSeries(RestClient client)
        {
            _client = client;
        }

        public async Task<List<SonarrSeries>> GetAllAsync()
        {
            var request = new RestRequest(endpoint, Method.GET);
            var response = await _client.ExecuteTaskAsync<List<SonarrSeries>>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK || response.Data == null)
                throw new ApplicationException("Failed to retrieve series list.");

            var data = response.Data;

            foreach (var item in data)
                item.ImageBaseUrl = _client.BaseUrl.OriginalString;

            return data;
        }

        public async Task DeleteAsync(int id)
        {
            var request = new RestRequest(endpoint + "/" + id, Method.DELETE);
            request.AddQueryParameter("deleteFiles", "true");

            var response = await _client.ExecuteTaskAsync<List<SonarrSeries>>(request);

            if ((response.StatusCode != System.Net.HttpStatusCode.OK &&
                 response.StatusCode != System.Net.HttpStatusCode.Accepted && 
                 response.StatusCode != System.Net.HttpStatusCode.NoContent))

                throw new ApplicationException("Failed to delete series.");
        }
    }
}
