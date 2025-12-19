using iText.Forms;
using iText.Forms.Fields;
using iText.Html2pdf;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Layout;
using iText.Layout.Element;
using TechTechie.Services.Common.Models;

namespace TechTechie.WebApi.Helpers
{
    public class PdfHeader : IEventHandler
    {
        //https://stackoverflow.com/questions/59654948/how-to-add-header-and-footer-to-a-pdf-with-itext-7
        string headerHtml;
        int headerHeight;
        public PdfHeader(string headerHtml, int headerHeight)
        {
            this.headerHtml = headerHtml;
            this.headerHeight = headerHeight;
        }

        public void HandleEvent(Event @event)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
            var page = docEvent.GetPage();
            var pageSize = page.GetPageSize();
            if (headerHeight == 0)
            {
                headerHeight = 100;
            }

            IList<IElement> headerElements = HtmlConverter.ConvertToElements(headerHtml);

            var canvas = new Canvas(page, new Rectangle(5, pageSize.GetHeight() - headerHeight, pageSize.GetWidth() - 10, headerHeight));

            foreach (var headerElement in headerElements)
            {
                canvas.Add((IBlockElement)headerElement);
            }
        }
    }

    public class PdfFooter : IEventHandler
    {
        string footerHtml;
        int footerHeight; // Variable to hold the height of the rectangle
        int totalPages;

        public PdfFooter(string footerHtml, int footerHeight, int totalPages)
        {
            this.footerHtml = footerHtml;
            this.totalPages = totalPages;
        }
        public void HandleEvent(Event @event)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
            PdfDocument pdfDoc = docEvent.GetDocument();
            var page = docEvent.GetPage();
            var pageSize = page.GetPageSize();

            if (footerHeight == 0)
            {
                footerHeight = 70;
            }
            IList<IElement> footerElements = HtmlConverter.ConvertToElements(footerHtml);

            var canvas = new Canvas(page, new Rectangle(5, 5, pageSize.GetWidth() - 10, footerHeight));


            // Add page number
            int pageNumber = pdfDoc.GetPageNumber(page);

            //Paragraph pageNumberParagraph = new Paragraph($"Page {pageNumber} of {totalPages}");
            // Paragraph pageNumberParagraph = new Paragraph($"Page {pageNumber} ");
            // pageNumberParagraph.SetFontSize(10);

            foreach (var footerElement in footerElements)
            {
                canvas.Add((IBlockElement)footerElement);
            }

            // Add page number to the footer
            // canvas.ShowTextAligned(pageNumberParagraph, pageSize.GetWidth(), 10, iText.Layout.Properties.TextAlignment.RIGHT);

        }
    }

    public class PdfTextLocator : LocationTextExtractionStrategy
    {

        public string TextToSearchFor { get; set; }
        public List<TextChunk> ResultCoordinates { get; set; }

        public static Rectangle GetTextCoordinates(PdfPage page, string s)
        {
            PdfTextLocator strat = new PdfTextLocator(s);
            PdfTextExtractor.GetTextFromPage(page, strat);
            foreach (TextChunk c in strat.ResultCoordinates)
            {
                if (c.Text == s)
                    return c.ResultCoordinates;
            }

            return null;
        }

        public static List<Rectangle> GetAllTextCoordinates(PdfPage page, string s)
        {
            PdfTextLocator strat = new PdfTextLocator(s);
            PdfTextExtractor.GetTextFromPage(page, strat);
            List<Rectangle> rectangles = new();

            foreach (TextChunk c in strat.ResultCoordinates)
            {
                if (c.Text == s)
                {
                    rectangles.Add(c.ResultCoordinates);
                    // c.Text = "";
                }
            }

            return rectangles;
        }
        public PdfTextLocator(string textToSearchFor)
        {
            this.TextToSearchFor = textToSearchFor;
            ResultCoordinates = new List<TextChunk>();
        }

        public override void EventOccurred(IEventData Data, EventType type)
        {
            if (!type.Equals(EventType.RENDER_TEXT))
                return;

            TextRenderInfo renderInfo = (TextRenderInfo)Data;
            IList<TextRenderInfo> text = renderInfo.GetCharacterRenderInfos();
            for (int i = 0; i < text.Count; i++)
            {
                if (text[i].GetText() == TextToSearchFor[0].ToString())
                {
                    string word = "";
                    for (int j = i; j < i + TextToSearchFor.Length && j < text.Count; j++)
                    {
                        word = word + text[j].GetText();
                    }

                    float startX = text[i].GetBaseline().GetStartPoint().Get(0);
                    float startY = text[i].GetBaseline().GetStartPoint().Get(1);
                    ResultCoordinates.Add(new TextChunk(word, new Rectangle(startX, startY, text[i].GetAscentLine().GetEndPoint().Get(0) - startX, text[i].GetAscentLine().GetEndPoint().Get(0) - startY)));
                }
            }
        }

    }
    public class TextChunk
    {
        public string Text { get; set; }
        public Rectangle ResultCoordinates { get; set; }
        public TextChunk(string s, Rectangle r)
        {
            Text = s;
            ResultCoordinates = r;
        }
    }
    public class PdfItext7Helper
    {
        //https://kb.itextpdf.com/home/it7kb/ebooks/itext-7-jump-start-tutorial-for-net/chapter-3-using-renderers-and-event-handlers-net
        //https://localhost:7146/api/GetHardCopyRequestPrint/pdf/1218/HardCopyRequestPrint?hardCopyRequestId=71&token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1lIjoiMyIsImNvbXBhbnkiOiJkZXZfcHJhdHVsIiwiZW1haWwiOiJzdXBwb3J0QHRlc3QuY29tIiwiZGF0ZUZvcm1hdCI6ImRkL01NL3l5eXkiLCJ0aW1lWm9uZSI6IkluZGlhIFN0YW5kYXJkIFRpbWUiLCJuYmYiOjE2NTQxNzE2NjUsImV4cCI6MTgxMTg1MTk2NSwiaWF0IjoxNjU0MTcxNjY2fQ.JInq2mlvGghoEqh8CPZ0IAYmIfPwhs8CfI2pRlxcLMk
        public static byte[] GetPdfBytesFromHtml(TemplateModel templateModel)
        {
            //https://stackoverflow.com/questions/51190687/how-to-add-a-signature-field-to-an-existing-pdf-a-document-keeping-pdf-a-3a-con

            byte[] byteArray = Array.Empty<byte>();


            float sign_x;
            float sign_y;
            float sign_width = 180;
            float sign_height = 80;

            var streamWriter = new MemoryStream();

            var wp = new WriterProperties();
            var writer = new PdfWriter(streamWriter, wp);

            var pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer);

            string topRightBottomLeftMargin = "5,5,5,5";

            var margins = topRightBottomLeftMargin.Split(',');

            int topMargin = 5, rightMargin = 5, bottomMargin = 5, leftMargin = 5;
            int pageWidth = 595, pageHeight = 842; // Default A4 size in points 

            if (margins.Length == 4)
            {
                topMargin = Convert.ToInt32(margins[0]);
                rightMargin = Convert.ToInt32(margins[1]);
                bottomMargin = Convert.ToInt32(margins[2]);
                leftMargin = Convert.ToInt32(margins[3]);

            }
            if (templateModel.page_header != "")
            {
                var headerHandler = new PdfHeader(templateModel.page_header, topMargin);
                pdfDoc.AddEventHandler(PdfDocumentEvent.START_PAGE, headerHandler);
            }
            if (templateModel.page_footer != "")
            {
                var footerHandler = new PdfFooter(templateModel.page_footer, bottomMargin, 0);
                pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, footerHandler);
            }

            var pageSize = new PageSize(pageWidth, pageHeight);

            //41 - Portrait
            //42 - Landscape

            if (templateModel.page_orientation_id.Value == 42)
            {
                pageSize = pageSize.Rotate();
            }

            pdfDoc.SetDefaultPageSize(pageSize);

            // Create ConverterProperties and set margins
            ConverterProperties properties = new ConverterProperties();

            // Given method doesnt apply document margins,
            HtmlConverter.ConvertToPdf(templateModel.page_body, pdfDoc, properties);

            var stramReader = new MemoryStream(streamWriter.ToArray());

            var rp = new ReaderProperties();
            var reader = new PdfReader(stramReader, rp);

            var streamWriter2 = new MemoryStream();
            streamWriter2.Write(streamWriter.ToArray(), 0, streamWriter.ToArray().Length);

            var writer2 = new PdfWriter(streamWriter2, wp);


            try
            {
                var pdfDocNew = new iText.Kernel.Pdf.PdfDocument(reader, writer2);


                for (int pageIndex = 1; pageIndex <= pdfDocNew.GetNumberOfPages(); pageIndex++)
                {
                    var pageToSearch = pdfDocNew.GetPage(pageIndex);


                    // Put input textbox 
                    var textBoxLocations = PdfTextLocator.GetAllTextCoordinates(pageToSearch, "[[textbox]]");

                    if (textBoxLocations != null && textBoxLocations.Count > 0)
                    {
                        for (int i = 0; i < textBoxLocations.Count; i++)
                        {
                            var textboxLoc = textBoxLocations[i];

                            var form = PdfAcroForm.GetAcroForm(pdfDocNew, true);
                            var textboxField = PdfFormField.CreateText(pdfDocNew, new Rectangle(textboxLoc.GetX(), textboxLoc.GetY() - 5, 100, 20));

                            if (textboxField != null)
                            {
                                textboxField.SetFieldName("textbox" + (i + 1).ToString());
                                form.AddField(textboxField, pageToSearch);
                            }
                        }
                    }

                    var signatureLocations = PdfTextLocator.GetAllTextCoordinates(pageToSearch, "[[digital-signature]]");

                    if (signatureLocations != null && signatureLocations.Count > 0)
                    {
                        for (int i = 0; i < signatureLocations.Count; i++)
                        {
                            var signatureLocation = signatureLocations[i];

                            var form = PdfAcroForm.GetAcroForm(pdfDocNew, true);
                            sign_x = signatureLocation.GetX();
                            sign_y = signatureLocation.GetY();

                            var signatureRectangle = new Rectangle(sign_x, sign_y, sign_width, sign_height);
                            var signatureField = PdfFormField.CreateSignature(pdfDocNew, signatureRectangle, PdfAConformanceLevel.PDF_A_3A);
                            signatureField.SetFieldName("digitalsignature" + (i + 1).ToString());
                            form.AddField(signatureField, pageToSearch);
                        }

                    }

                }
                // Merge Pdf document

                // mergePdfUrl = "https://api.assetinfinity.io/api/GetPurchaseOrderPrint/pdf/1/DEF_PO?LoginUser=1285&purchaseOrderId=28&token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IjMiLCJjb21wYW55IjoiY29sbGllcnNwbyIsImVtYWlsIjoic3VwcG9ydEBwY3NpbmZpbml0eS5pbiIsImRhdGVGb3JtYXQiOiJkZC9NTS95eXl5IGhoOm1tIHR0IiwidGltZVpvbmUiOiJJbmRpYSBTdGFuZGFyZCBUaW1lIiwiZGV2aWNlSWQiOiIiLCJ1c2VyVHlwZUlkIjoiNSIsIm5iZiI6MTY5NDE3MjY5MCwiZXhwIjoxODUxODUyOTkwLCJpYXQiOjE2OTQxNzI2OTB9.uOd1ik68Bj4CPwbr7584dy5qVsMUkjRsfAeDriQkIww";

                try
                {
                    if (!string.IsNullOrEmpty(templateModel.merge_pdf_url))
                    {
                        byte[] downloadedPdfBytes = null;
                        Task.Run(async () => downloadedPdfBytes = await DownloadPdfAsync(templateModel.merge_pdf_url)).Wait();

                        pdfDocNew.Close();
                        byteArray = streamWriter2.ToArray();

                        List<byte[]> pdfBytes = new List<byte[]>();

                        pdfBytes.Add(byteArray);
                        pdfBytes.Add(downloadedPdfBytes);

                        MemoryStream fileStream = new ITextsharpHelper().MergePDFs(pdfBytes);

                        byteArray = fileStream.ToArray();

                        fileStream.Close();
                    }
                    else
                    {
                        pdfDocNew.Close();
                        byteArray = streamWriter2.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    string error = ex.Message;

                    pdfDocNew.Close();
                    byteArray = streamWriter2.ToArray();
                }

            }
            catch (Exception ex)
            {
                ///itext7 error trailer not found when conveting html to pdf C# comes on custom size of page
                string error = ex.Message;
            }

            if (!string.IsNullOrEmpty(templateModel.water_mark_text))
            {
                byteArray = ApplyWatermark(byteArray, templateModel.water_mark_text);
            }

            return byteArray;
        }
        private static async Task<byte[]> DownloadPdfAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    throw new Exception("Failed to download PDF.");
                }
            }
        }

        private static byte[] ApplyWatermark(byte[] bytes, string watermarkText)
        {
            iTextSharp.text.pdf.BaseFont baseFont = iTextSharp.text.pdf.BaseFont.CreateFont(iTextSharp.text.pdf.BaseFont.TIMES_ROMAN, iTextSharp.text.pdf.BaseFont.CP1252, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);

            using (var ms = new MemoryStream(10 * 1024))
            {
                using (var reader = new iTextSharp.text.pdf.PdfReader(bytes))
                using (var stamper = new iTextSharp.text.pdf.PdfStamper(reader, ms))
                {
                    var pages = reader.NumberOfPages;
                    for (var i = 1; i <= pages; i++)
                    {
                        var dc = stamper.GetOverContent(i);
                        AddWaterMarkText(dc, watermarkText, baseFont, 50, 45, iTextSharp.text.BaseColor.GRAY, reader.GetPageSizeWithRotation(i));
                    }
                    stamper.Close();
                }
                return ms.ToArray();
            }
        }
        private static void AddWaterMarkText(iTextSharp.text.pdf.PdfContentByte pdfData, string watermarkText, iTextSharp.text.pdf.BaseFont font, float fontSize, float angle, iTextSharp.text.BaseColor color, iTextSharp.text.Rectangle realPageSize)
        {
            var gstate = new iTextSharp.text.pdf.PdfGState { FillOpacity = 0.35f, StrokeOpacity = 0.3f };
            pdfData.SaveState();
            pdfData.SetGState(gstate);
            pdfData.SetColorFill(color);
            pdfData.BeginText();
            pdfData.SetFontAndSize(font, fontSize);
            var x = (realPageSize.Right + realPageSize.Left) / 2;
            var y = (realPageSize.Bottom + realPageSize.Top) / 2;
            pdfData.ShowTextAligned(iTextSharp.text.Element.ALIGN_CENTER, watermarkText, x, y, angle);
            pdfData.EndText();
            pdfData.RestoreState();
        }
        public static string GetPdfHexFromHtml(TemplateModel templateModel)
        {
            byte[] buffer = GetPdfBytesFromHtml(templateModel);

            System.Text.StringBuilder sb = new();
            foreach (byte b in buffer)
                sb.Append(b.ToString("X2"));

            return sb.ToString();

        }

    }
}
