using NUnit.Framework;
using System.Threading.Tasks;
using YoutubeDownloader;
namespace YoutubeDownloaderTest
{
    public class YoutubeVideoTest
    {
        string url = "https://youtube.com/watch?v=u_yIGGhubZs";
        string path = @"C:\Users\Admin\source\repos\YoutubeDownloader\YoutubeDownloader\bin\Debug\netcoreapp3.1";
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            string[] args = new string[] { url,path  };
            Task<string> task = YoutubeVideo.DownloadYoutubeInformation(args);
            task.Wait();
            path = task.Result;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            //Delete Directory
            System.IO.Directory.Delete(path, true);
        }

        [Test]
        public void IsVideoExist()
        {
            Assert.IsTrue(System.IO.File.Exists($"{path}\\vCollections - Blender 2.80 Fundamentals.mp4"));
        }

        [Test]
        public void IsDescriptionExist()
        {
            Assert.IsTrue(System.IO.File.Exists($"{path}\\info.txt"),"The file doesn't exist");
        }

        [Test]
        public void IsClosedCaptionFileExist()
        {
            Assert.IsTrue(System.IO.File.Exists($"{path}\\Collections - Blender 2.80 Fundamentals.srt"), "The file doesn't exist");
        }
    }

    public class YoutubePlaylistTest
    {
        readonly string url = "https://www.youtube.com/playlist?list=PLL0CQjrcN8D37iAA8C5zrpAfkUbyhfKT3";
        readonly string path = @"C:\Users\Admin\source\repos\YoutubeDownloader\YoutubeDownloader\bin\Debug\netcoreapp3.1";
        readonly int numberOfVideos = 2;
        readonly string title = "SiIvaGunner's Original Fusion Collabs";
        string newPath;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            newPath = $"{path}/{title}";
            YoutubePlaylist.DownloadPlayListInformation(url, path, numberOfVideos).Wait();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            System.IO.Directory.Delete(newPath, true);
        }
        [Test]
        public void IsPlaylistFolderExist()
        { 
            Assert.IsTrue(System.IO.Directory.Exists(newPath), $"The directory {title} doesn't exist at {path}");
        }

        [Test]
        public void IsNumberOfFoldersCorrect()
        {
            var directories = System.IO.Directory.GetDirectories(newPath);
            int lengthOfDirectories = directories.Length;
            Assert.IsTrue(lengthOfDirectories == numberOfVideos);
        }
    }
}