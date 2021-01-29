using System;
using System.IO;
using System.Collections.Generic;
using YoutubeExplode;
using System.Threading.Tasks;
using YoutubeExplode.Videos.Streams;
using System.Diagnostics;

namespace YoutubeDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string url = args[0];
                string path = args[1];
                bool isPlayList = (url.ToLower().Contains("list"));
                string stringCount = (args.Length == 3) ? args[2] : "0";
                int count = int.TryParse(stringCount, out count) ? count : 0; //in case try parse fails
                if (isPlayList) { YoutubePlaylist.DownloadPlayListInformationOneAtATime(url, path, count).Wait(); }
                else { YoutubeVideo.DownloadYoutubeInformation(args).Wait(); }
                Console.WriteLine("Task Completed");
            }
            catch(Exception e)
            {
                Console.WriteLine("Error");
                Console.WriteLine(e);
                Console.WriteLine();
                Console.WriteLine("There was a problem with the inputs. \n" +
                    "Input ->  Required { url:string path:string } || NOT-Required {count:int} ");

                Console.WriteLine();
                Console.WriteLine("The inputs taken in.");
                for(int i = 0; i < args.Length; i++)
                {
                    Console.WriteLine($"{i} : {args[i]}");
                }
            }
        }
    }
    public static class YoutubeVideo 
    {
        public static async Task<string> DownloadYoutubeInformation(string[] args)
        {
            var youtube = new YoutubeClient();

            // You can specify video ID or URL
            var video = await youtube.Videos.GetAsync(args[0]);
            var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(args[0]);

            Debug.WriteLine("Collecting Title, Author and Description");
            Console.WriteLine("Collecting Metadata");
            var title = video.Title; // "Collections - Blender 2.80 Fundamentals"
            var author = video.Author; // "Blender"
            var description = video.Description;

            
            string newPath = $"{args[1]}/{title}/";
            Debug.WriteLine($"Directory |{newPath}| is being created if it doesn't exist");
            Console.WriteLine($"Directory [{newPath}] creation started");
            System.IO.FileInfo file = new System.IO.FileInfo(newPath);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
            _ = File.WriteAllLinesAsync($"{newPath}/info.txt", new string[] { title, author, description });
            Debug.WriteLine($"Directory {newPath} has been created if it doesn't exist");
            Console.WriteLine("Directory creation completed");

            Console.WriteLine("Collecting manifest information starting");
            //Extract manifest information
            int start = args[0].IndexOf("=") + 1;
            int end = args[0].IndexOf("&");
            string manifestString = args[0].Substring(start, (end == -1 ? args[0].Length - start : end - start) );
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(manifestString);
            Console.WriteLine("Collecting manifest information ended");

            Console.WriteLine("Started Downloading files");
            //Download HIghest Quality Video
            Task highVid = DownloadVideoOnly(youtube, streamManifest, title, newPath);
            //Download Highest Quality Audio
            Task highAud = DownloadAudioOnly(youtube, streamManifest, title, newPath);
            //Download Muxed Vided
            Task highMux = DownloadMuxOnly(youtube, streamManifest, title, newPath);
            //Download Closed Caption
            Task closedCaption = DownloadSubtitles(youtube, trackManifest, title, newPath);

            Task.WaitAll(new Task[] { highAud, highMux, highVid, closedCaption });
            Trace.WriteLine("Files download completed");
            return newPath;

        }

        public static async Task DownloadAudioVideoMuxButNoCaption(YoutubeExplode.Videos.Video video, string path)
        {
            var youtube = new YoutubeClient();

            var title = video.Title; // "Collections - Blender 2.80 Fundamentals"
            Console.WriteLine($"This video {title} is downloading");
            var author = video.Author; // "Blender"
            var description = video.Description;
            string id = video.Id.ToString();

            string newPath = $"{path}/{title}/";
            System.IO.FileInfo file = new System.IO.FileInfo(newPath);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
            _ = File.WriteAllLinesAsync($"{newPath}/info.txt", new string[] { title, author, description });

            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(id);

            //Download HIghest Quality Video
            Task highVid = DownloadVideoOnly(youtube, streamManifest, title, newPath);
            //Download Highest Quality Audio
            Task highAud = DownloadAudioOnly(youtube, streamManifest, title, newPath);
            //Download Muxed Vided
            Task highMux = DownloadMuxOnly(youtube, streamManifest, title, newPath);

            Console.WriteLine($"This video {title} is completed");
            Task.WaitAll(new Task[] { highAud, highMux, highVid});
        }

        static async Task DownloadVideoOnly(YoutubeClient youtube,StreamManifest streamManifest, string title, string path)
        {
            var highVideoQuality = streamManifest.GetVideoOnly().WithHighestVideoQuality();
            if (highVideoQuality != null)
            {
                string newPath = $"{path}/v{title}.{highVideoQuality.Container}";
                await youtube.Videos.Streams.DownloadAsync(highVideoQuality, newPath);
            }
        }

        static async Task DownloadAudioOnly(YoutubeClient youtube, StreamManifest streamManifest, string title, string path)
        {
            var highAudioQuality = streamManifest.GetAudioOnly().WithHighestBitrate();
            if (highAudioQuality != null)
            {
                string newPath = $"{path}/a{title}.{highAudioQuality.Container}";
                await youtube.Videos.Streams.DownloadAsync(highAudioQuality, newPath);
            }
        }

        static async Task DownloadMuxOnly(YoutubeClient youtube, StreamManifest streamManifest, string title, string path)
        {
            var highMuxQuality = streamManifest.GetMuxed().WithHighestVideoQuality();
            if (highMuxQuality != null)
            {
                string newPath = $"{path}/m{title}.{highMuxQuality.Container}";
                await youtube.Videos.Streams.DownloadAsync(highMuxQuality, newPath);
            }
        }   
    
        static async Task DownloadSubtitles(YoutubeClient youtube, YoutubeExplode.Videos.ClosedCaptions.ClosedCaptionManifest closedCaptionManifest, string title, string path)
        {
            var trackInfo = closedCaptionManifest.TryGetByLanguage("en");

            if (trackInfo != null)
            {
                string newPath = $"{path}/{title}.srt";
                await youtube.Videos.ClosedCaptions.DownloadAsync(trackInfo, newPath);
            }
        }
    }

    public static class YoutubePlaylist
    {
        public static async Task DownloadPlayListInformation(string playlistUrl, string path, int count = 0)
        {
            var youtube = new YoutubeClient();

            // Get playlist metadata
            var playlist = await youtube.Playlists.GetAsync(playlistUrl);

            var title = playlist.Title; 
            var author = playlist.Author;
            var description = playlist.Description;

            IReadOnlyList<YoutubeExplode.Videos.Video> somePlayListVideos = default; //
            /*
             * It is possible to just call the playlist onetime and use the GetAsyncEnumerator
             * then using that count/length property to figure which is smaller.
             * The lazy option was choosen instead
             */
            try
            {
                //if the count is equal to zero move to the catch to get all the videos
                if (count == 0) {  throw new Exception(); }
                // Get videos from 0 to the count
                somePlayListVideos = await youtube.Playlists
                    .GetVideosAsync(playlist.Id)
                    .BufferAsync(count);
            }
            catch
            {
                somePlayListVideos = await youtube.Playlists.GetVideosAsync(playlist.Id);
            }
            finally
            {
                string newPath = $"{path}/{title}/";
                System.IO.FileInfo file = new System.IO.FileInfo(newPath);
                file.Directory.Create(); // If the directory already exists, this method does nothing.
                _ = File.WriteAllLinesAsync($"{file.FullName}/playlist-info.txt", new string[] { title, author, description });

                Parallel.ForEach(somePlayListVideos, (video) =>
                {
                    YoutubeVideo.DownloadAudioVideoMuxButNoCaption(video, newPath).Wait();
                });
            };
        }

        public static async Task DownloadPlayListInformationOneAtATime(string playlistUrl, string path, int count = 0, int waitTime = 500)
        {
            var youtube = new YoutubeClient();

            // Get playlist metadata
            var playlist = await youtube.Playlists.GetAsync(playlistUrl);

            var title = playlist.Title;
            Console.WriteLine($"Downloading Playlist: {title}");
            var author = playlist.Author;
            var description = playlist.Description;

            IReadOnlyList<YoutubeExplode.Videos.Video> somePlayListVideos = default; //
            /*
             * It is possible to just call the playlist onetime and use the GetAsyncEnumerator
             * then using that count/length property to figure which is smaller.
             * The lazy option was choosen instead
             */
            try
            {
                //if the count is equal to zero move to the catch to get all the videos
                if (count == 0) { throw new Exception(); }
                // Get videos from 0 to the count
                somePlayListVideos = await youtube.Playlists
                    .GetVideosAsync(playlist.Id)
                    .BufferAsync(count);
            }
            catch
            {
                somePlayListVideos = await youtube.Playlists.GetVideosAsync(playlist.Id);
            }
            finally
            {
                string newPath = $"{path}/{title}/";
                System.IO.FileInfo file = new System.IO.FileInfo(newPath);
                file.Directory.Create(); // If the directory already exists, this method does nothing.
                _ = File.WriteAllLinesAsync($"{file.FullName}/playlist-info.txt", new string[] { title, author, description });

                List<Task> tempTaskList = new List<Task>();
                for (int i = 0; i < somePlayListVideos.Count;i++)
                {
                    Console.WriteLine($"Starting videos {i+1} of {somePlayListVideos.Count}");
                    YoutubeVideo.DownloadAudioVideoMuxButNoCaption(somePlayListVideos[i], newPath).Wait();
                    System.Threading.Thread.Sleep(waitTime);
                }
            };
        }
    }
}

