using System.Text.Json;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

var configText = File.ReadAllText("appsettings.json");
System.Console.WriteLine(configText);
var config = JsonSerializer.Deserialize<Config>(configText);
if (config is null)
    throw new NullReferenceException("config is null");

using (var client = new ImapClient())
{
    client.Connect(config.ImapServer, config.ImapPort, SecureSocketOptions.SslOnConnect);

    client.Authenticate(config.Email, config.Password);

    var folder = client.GetFolder(config.FolderName);
    folder.Open(FolderAccess.ReadOnly);

    var uids = folder.Search(SearchQuery.All);

    var items = folder.Fetch(uids, MessageSummaryItems.UniqueId | MessageSummaryItems.BodyStructure)
    .OrderBy(m => m.UniqueId);
    int i = 0;
    foreach (var item in items)
    {
        foreach (var attachment in item.BodyParts)
        {
            try
            {

                var entity = folder.GetBodyPart(item.UniqueId, attachment);
                var part = (MimePart)entity;
                if (part.ContentType.MimeType is null
                || !part.ContentType.MimeType.Contains("image"))
                    continue;
                System.Console.WriteLine(i);
                var date = item.Date.ToString();
                using (var stream = File.Create("images/" + i + ".jpg"))
                    part.Content.DecodeTo(stream);
                i++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
    client.Disconnect(true);
}