﻿using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using repostpolice;
using repostpolice.Utility;
using skypebot.Services.couchpotato.model;
using SKYPE4COMLib;

namespace skypebot.Services.couchpotato
{
    public class CouchPotatoService : IChatBotService
    {
        private readonly Dictionary<string, IEnumerable<string>> _userAllowedMovieIds;
        public string[] Commands { get; } = { "addmovie", "getmovie" };
        private const string BestQuality = "6a1d1912dc7a48c6819ac95053892932";
        private readonly string _couchpotatoApiKey;
        private readonly string _couchpotatoUrl;
        private readonly IAuthorizationManager _authorizationManager;

        public CouchPotatoService(int priority, IAuthorizationManager authorizationManager1)
        {
            _couchpotatoApiKey = ConfigurationManager.AppSettings["couchpotato_apikey"];
            _couchpotatoUrl = ConfigurationManager.AppSettings["couchpotato_url"];
            Priority = priority;
            _authorizationManager = authorizationManager1;
            _userAllowedMovieIds = new Dictionary<string, IEnumerable<string>>();
        }

        public int Priority { get; private set; }
        public bool CanHandleCommand(string command)
        {
            return Commands.Contains(command);
        }

        public void HandleCommand(string fromHandle, string fromDisplayName, string command, string parameters)
        {
            if (!_authorizationManager.HasPermission(fromHandle, this.GetType().Name.ToLower())) return;
            switch (command)
            {
                case "addmovie":
                    TryAddMovie(fromHandle, fromDisplayName, parameters.Split(' ')[0]);
                    break;
                case "getmovie":
                    break;
                default:
                    return;

            }
        }

        public void HandleCommand(ChatMessage msg, string command, string parameters)
        {

        }

        private async void TryAddMovie(string fromHandle, string fromDisplayName, string movie)
        {
            var movieId = movie.Substring(0, 100);
            if (movieId.StartsWith("tt"))
            {
                AddMovie(fromHandle, fromDisplayName, movieId);
            }
            else
            {
                SearchMovie(fromDisplayName, movie);
            }
        }

        private async void SearchMovie(string fromDisplayName, string movie)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                const string command = "/search?";
                var res =
                    await
                        httpClient.GetStringAsync($"{_couchpotatoUrl}{_couchpotatoApiKey}{command}q={movie}&type=movie");
                var content = Newtonsoft.Json.JsonConvert.DeserializeObject<MoviesModel>(res);
                if (content.Movies != null)
                {
                    ChatBot.EnqueueMessage(
                   $"{fromDisplayName}: Found the following movies, please add one using \"!addmovie tt0000000\" from imdb-id below\n" +
                   $"{string.Join("\n", content.Movies.Select(x => $"Title: {x.original_title} ImdbId: {x.imdb} Year: {x.year} ImdbUrl: {x.ImdbUrl}").ToList())}");
                }
                var movies = new List<Movie>();




            }
        }

        private async void AddMovie(string fromHandle, string fromDisplayName, string movieIdentifier, string quality = BestQuality)
        {

            if (!Regex.IsMatch(movieIdentifier, @"tt\d{7}")) return;
            if (!_userAllowedMovieIds[fromHandle].Contains(movieIdentifier)) return;
            using (HttpClient httpClient = new HttpClient())
            {
                const string command = "/movie.add?";

                var res = await
                    httpClient.GetStringAsync(
                        $"{_couchpotatoUrl}{_couchpotatoApiKey}{command}identifier={movieIdentifier}&profile_id={quality}");
                dynamic content = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(res);
                if (content.success)
                {

                    ChatBot.EnqueueMessage($"{fromDisplayName}: Your movie was successfully added!");
                }
            }
        }
    }
}