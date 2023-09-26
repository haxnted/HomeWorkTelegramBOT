using Telegram.Bot.Types;
using Telegram.Bot;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Text;

namespace HomeWorkHub
{
	public class PdfConverter
	{
        private Dictionary<long, List<PhotoSize>> userPhotos = new Dictionary<long, List<PhotoSize>>();
		private string _pdfpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\HomeWork\\PdfFiles";
		public PdfConverter()
		{
			if (!Directory.Exists(_pdfpath))
	    		Directory.CreateDirectory(_pdfpath);
		}
        
        public bool IsUserContains(long userId) =>
			userPhotos.ContainsKey(userId);
        public void RemoveData(long id) =>
			userPhotos[id].Clear();
		
		public void AddPhoto(long id, Message message)
		{
			if (!IsUserContains(id))
				userPhotos[message.Chat.Id] = new List<PhotoSize>();
			
            userPhotos[message.Chat.Id].Add(message?.Photo?.LastOrDefault() ?? throw new NullReferenceException("Error addPhoto"));
        }
		private List<PhotoSize> GetUserPhotos(long id) =>
            new(userPhotos[id]);
        public int GetCountPhotos(long id)
        {
            if (!IsUserContains(id))
                return 0;  
            return GetUserPhotos(id).Count;
        }
        public string GetDocument(long id, string filename, ITelegramBotClient botClient)
        {
            string fullpath = "";
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using PdfDocument document = new PdfDocument();
                List<PhotoSize> photos = GetUserPhotos(id);

                foreach (PhotoSize photo in photos)
                {
                    PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    using MemoryStream stream = new MemoryStream();
                    botClient.GetInfoAndDownloadFileAsync(photo.FileId, stream).Wait();
                    stream.Seek(0, SeekOrigin.Begin);

                    XImage image = XImage.FromStream(stream);
                    gfx.DrawImage(image, 0, 0, page.Width, page.Height);
                    stream.Close();
                }

                fullpath = Path.Combine(_pdfpath, filename.Contains(".pdf") ? filename : filename + ".pdf");
                
                document.Save(fullpath);
                document.Close();
                RemoveData(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return fullpath;
        }
		

	}
}
