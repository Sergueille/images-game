using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public partial class InternetMachine : Node
{
    const string HTML_REGEX_PREFIX = "mediaurl=";
    const string HTML_TMP_FILE = "user://tmp.html";

    class MatchComparer : IEqualityComparer<Match>
    {
        public bool Equals(Match x, Match y)
        {
            return cleanUrl(x.Value) == cleanUrl(y.Value);
        }

        public int GetHashCode(Match obj)
        {
            return cleanUrl(obj.Value).GetHashCode();
        }
    }

    [Export] public float requestTimeout = 5.0f;

    private Action<Image, string> onCompleted;
    private Action onFailure;
    private Action<string> onLog;
    private ImageFilter[] filters;
    private ImageFilter[] lastChanceFilters;

    private List<(Image, string)> lastChanceImages;
    
    private bool resultGiven;
    private bool isBusy = false;
    private HashSet<string> visitedUrls = new HashSet<string>();

    private Dictionary<string, string> cookies;

    public void RequestImage(string query, Action<Image, string> onCompleted, Action onFailure, ImageFilter[] filters, ImageFilter[] lastChanceFilters, Action<string> onLog)
    {
        CancelAllRequests();

        string queryCompleted = $"{query} photo png";
        string queryEscaped = System.Net.WebUtility.UrlEncode(queryCompleted);

        this.onCompleted = onCompleted;
        this.onFailure = onFailure;
        this.onLog = onLog;
        this.filters = filters;
        this.lastChanceFilters = lastChanceFilters;
        isBusy = true;

        lastChanceImages = new List<(Image, string)>();

        string fullQuery = $"https://www.bing.com/images/search?q={queryEscaped}&qft=%20filterui%3Aphoto-transparent";

        GD.Print(fullQuery);
        DoRequest(fullQuery, 3, HandleSearchPageResult, () => { onFailure(); }, true);
    }

    private void HandleSearchPageResult(byte[] body)
    {
        string html = System.Text.Encoding.UTF8.GetString(body); // A bunch of html nonsense
        Regex r = new Regex(HTML_REGEX_PREFIX + "([^&]*)");

        FileAccess f = FileAccess.Open(HTML_TMP_FILE, FileAccess.ModeFlags.Write);
        f.StoreString(html);
        
        Match[] matches = r.Matches(html).Distinct(new MatchComparer()).ToArray();
        matches = matches.Where(m => !visitedUrls.Contains(cleanUrl(m.Value))).ToArray(); // Filter already visited urls

        resultGiven = false;

        if (matches.Length == 0)
        {
            onLog("0 results");
            onFailure();
            isBusy = false;
        }

        GD.Print("Result count:", matches.Length);

        Random.Shared.Shuffle(matches);

        FetchImage(matches, 0);
    }

    private void FetchImage(Match[] urls, int index)
    {
        if (index >= urls.Length) {
            if (lastChanceImages.Count > 0)
            {
                visitedUrls.Add(lastChanceImages[0].Item2);
                ReturnResult(lastChanceImages[0].Item1);
                return;
            }
            else
            {
                onFailure(); 
                isBusy = false;
                return; 
            }
        }

        Match m = urls[index];

        string uglyLink = m.Value;
        string cleanedLink = cleanUrl(uglyLink);

        bool ok = true;

        foreach (ImageFilter filter in filters)
        {
            if (!filter.FilterUrl(cleanedLink))
            {
                GD.Print("Url rejected by ", filter.GetName(), ": ", cleanedLink);
                onLog("url rejected");
                ok = false;
                continue;
            }
        }

        if (!cleanedLink.Contains(".png"))
        {
            GD.Print("Url rejected because not a PNG: ", cleanedLink);
            onLog("err not png");
            ok = false;
        }

        if (ok)
        {
            DoRequest(cleanedLink, 1,
                async (body) => {
                    bool ok = await HandleImageResult(body, cleanedLink);

                    if (!ok)
                        FetchImage(urls, index + 1);
                }, 
                () => {
                    FetchImage(urls, index + 1);
                },
                true
            );
        }
        else
        {
            visitedUrls.Add(cleanedLink);
            FetchImage(urls, index + 1);
        }
    }

    private async Task<bool> HandleImageResult(byte[] body, string link)
    {
        (int resultCode, Image img) = await HandleImageResultAsync(body);

        if (resultCode == 0)
        {
            GD.Print("Failed to parse image: ", link);
            onLog("img corrupt");
            visitedUrls.Add(link);
            return false;
        }
        else if (resultCode == 1)
        {
            GD.Print("Image rejected by filter: ", link);
            onLog("img rejected");
            visitedUrls.Add(link);
            return false;
        }
        else if (resultCode == 3)
        {
            GD.Print("Image used for last chance: ", link);
            onLog("img bad");
            lastChanceImages.Add((img, link));
            return false;
        }

        visitedUrls.Add(link);
        ReturnResult(img);
        return true;
    }

    private void ReturnResult(Image img)
    {
        CancelAllRequests();

        if (!resultGiven)
        {
            resultGiven = true;
            onCompleted(img, "url not available :(");
            isBusy = false;
        }
    }

    private Task<(int, Image)> HandleImageResultAsync(byte[] body)
    {
        TaskCompletionSource<(int, Image)> task = new TaskCompletionSource<(int, Image)>();

        Thread t = new Thread(() => {
            Image img = ImageFromData(body);

            if (img == null)
            {
                task.SetResult((0, null));
                return;
            }
            else
            {
                foreach (ImageFilter filter in filters)
                {
                    if (!filter.FilterImage(img))
                    {
                        task.SetResult((1, null));
                        return;
                    }
                }
                
                foreach (ImageFilter filter in lastChanceFilters)
                {
                    if (!filter.FilterImage(img))
                    {
                        task.SetResult((3, img));
                        return;
                    }
                }
            }

            task.SetResult((2, img));
        });

        t.Start();

        return task.Task;
    }

    public Image ImageFromData(byte[] data, string format="png")
    {
        Image img;

        try
        {
            FileAccess f = FileAccess.Open($"user://tmp{data.GetHashCode()}.{format}", FileAccess.ModeFlags.Write);
            f.StoreBuffer(data);
            f.Close();

            img = Image.LoadFromFile(f.GetPath());

            f.Close();
            DirAccess.RemoveAbsolute(f.GetPathAbsolute());
        }
        catch
        {
            return null;
        }

        if (img == null) { return null; }

        // Crop the image
        Vector2I size = img.GetSize();
        Vector2I min = new Vector2I(int.MaxValue, int.MaxValue);
        Vector2I max = new Vector2I(int.MinValue, int.MinValue);

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                if (img.GetPixel(x, y).A > 0.1)
                {
                    if (x < min.X) min.X = x;
                    if (x > max.X) max.X = x;
                    if (y < min.Y) min.Y = y;
                    if (y > max.Y) max.Y = y;
                }
            }
        }

        Image cropped = Image.CreateEmpty(max.X - min.X, max.Y - min.Y, true, Image.Format.Rgba8);
        Vector2I newSize = cropped.GetSize();
        for (int x = 0; x < newSize.X; x++)
        {
            for (int y = 0; y < newSize.Y; y++)
            {
                cropped.SetPixel(x, y, img.GetPixel(min.X + x, min.Y + y));
            }
        }

        cropped.ClearMipmaps();
        cropped.GenerateMipmaps();

        return cropped;
    }

    private void CancelAllRequests()
    {
        foreach (Node n in GetChildren())
        {
            if (n is HttpRequest req)
            {
                req.CancelRequest();
                n.QueueFree();
            }
        }
    }


    public void DoRequest(string url, int maxRetry, Action<byte[]> onOk, Action onFailed, bool log)
    {
        string shortUrl = url;
        shortUrl = shortUrl.Replace("https://", "").Replace("http://", "").Replace("www.", "");
        if (shortUrl.Length > 10) { shortUrl = shortUrl[..8]; }
        if (log) { onLog("get " + shortUrl); }

        HttpRequest rec = new HttpRequest();
        AddChild(rec); 

        rec.RequestCompleted += (result, responseCode, headers, body) =>
        {
            rec.QueueFree();

            if (result == (long)HttpRequest.Result.Success && responseCode == 200)
            {
                if (log) { onLog($"HTTP 200 ok"); }

                bool hasCookieBefore = cookies.Count == 0;

                // Store cookies to make bing believe we're a good client
                string[] setCookieInstructions = headers
                    .Where(h => h.ToLower().Contains("set-cookie:"))
                    .Select(h => h.Split(":")[1].Split(";")[0])
                    .ToArray();

                foreach (string instr in setCookieInstructions)
                {
                    GD.Print(instr);
                    string cookieName = instr.Split("=")[0].Trim();
                    string cookieValue = instr.Replace(cookieName + "=", "").Trim();
                    cookies[cookieName] = cookieValue;
                }

                if (hasCookieBefore && cookies.Count != 0) // New cookies added, retry
                {
                    DoRequest(url, maxRetry, onOk, onFailed, log);
                    return;
                }

                onOk(body);
            }
            else
            {
                if (result != (long)HttpRequest.Result.Success)
                {
                    GD.Print("Invalid result code ", result, " for: ", url);
                    if (log) { onLog($"error {result}"); }
                }
                else
                {
                    GD.Print("Invalid response code ", responseCode, " for: ", url);
                    if (log) { onLog($"HTTP err {responseCode}"); }
                }

                if (maxRetry > 0)
                {
                    DoRequest(url, maxRetry - 1, onOk, onFailed, log);
                }
                else
                {
                    onFailed();
                }
            }
        };

        if (cookies == null) { cookies = new Dictionary<string, string>(); }

        string cookieHeader = "Cookie:" + string.Join("; ", cookies.Select(pair => $"{pair.Key}={pair.Value}"));
        GD.Print(cookieHeader);

        // Pretend to be firefox by sending a bunch of random headers
        rec.Timeout = requestTimeout;
        rec.Request(url, customHeaders: [
            "User-Agent: Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:151.0) Gecko/20100101 Firefox/151.0",
            "Accept-Language: en-US,en",
            "Pragma: no-cache",
            "Cache-Control: no-cache",
            "Sec-Fetch-Dest: document",
            "Sec-Fetch-Mode: navigate",
            "Sec-Fetch-Site: none",
            "Sec-Fetch-User: ?1",
            cookieHeader,
        ]);
    }

    private static string cleanUrl(string url)
    {
         return url
                .Replace(HTML_REGEX_PREFIX, "")
                .ToLower()
                .Replace("&amp;", "&")
                .Replace("%3a", ":")
                .Replace("%2f", "/")
                .Replace("%3f", "?")
                .Replace("%3d", "=")
                .Replace("%26", "&")
                .Replace("%25", "%");
    }

    public bool IsBusy()
    {
        return isBusy;
    }

    public void ResetVisitedUrls()
    {
        visitedUrls.Clear();
    }
}

public interface ImageFilter
{
    public bool FilterUrl(string url);
    public bool FilterImage(Image img);
    public string GetName();
}

/// <summary>
/// Checks that the image is transparent
/// </summary>
public class TransparencyFilter : ImageFilter
{
    public bool FilterUrl(string url)
    {                
        return true;
    }

    public bool FilterImage(Image img)
    {
        Vector2I size = img.GetSize();
        Color[] corners = [
            img.GetPixel(0, 0),
            img.GetPixel(0, size.Y-1),
            img.GetPixel(size.X-1, 0),
            img.GetPixel(size.X-1, size.Y-1)
        ];

        foreach (Color c in corners)
        {
            if (c.A >= 0.1)
            {
                GD.Print("Images refused because corners weren't transparent!");
                return false;
            }
        }

        return true;
    }

    public string GetName()
    {
        return "transparency filter";
    }
}

/// <summary>
/// Checks that the image is a photo
/// </summary>
public class PhotoFilter : ImageFilter
{
    public bool FilterUrl(string url)
    {                
        return !(
            url.Contains("ai-generated") ||
            url.Contains("silhouette") ||
            url.Contains("logo") ||
            url.Contains("hand-drawn") ||
            url.Contains("pixel") ||
            url.Contains("icon") ||
            url.Contains("vector") ||
            url.Contains("illustration") ||
            url.Contains("profile") ||
            url.Contains("painting") ||
            url.Contains("sticker") ||
            url.Contains("svg")
        );;
    }

    public bool FilterImage(Image img)
    {
        // Tried:
        //   - counting different colors

        int badPixels = 0;
        int nonTransparentPixels = 0;
        Vector2I size = img.GetSize();
        for (int x = 0; x < size.X - 1; x++)
        {
            for (int y = 0; y < size.Y - 1; y++)
            {
                Color c = img.GetPixel(x, y);
                if (c.A > 0.1)
                {
                    Color up = img.GetPixel(x, y+1);
                    Color right = img.GetPixel(x+1, y);

                    if (AreColorsEqual(up, right) && AreColorsEqual(up, right))
                    {
                        badPixels += 1;
                    }

                    nonTransparentPixels += 1;
                }
            }
        }

        float badPixelsRatio = badPixels / (float)nonTransparentPixels;

        if (badPixelsRatio > 0.4)
        {
            GD.Print("Images refused because too many pixels were equal to their neighbors!");
            return false;
        }

        return true;
    }

    public string GetName()
    {
        return "photo filter";
    }

    private bool AreColorsEqual(Color a, Color b)
    {
        return 
            a.R8 == b.R8 &&
            a.G8 == b.G8 &&
            a.B8 == b.B8;
    }
}

/* TODO

/// <summary>
/// Checks that the image is a single image
/// </summary>
public class SingleImageFilter : ImageFilter
{
    public bool FilterImage(Image img)
    {
        Vector2I size = img.GetSize();
        int croppedStart = 0; int croppedEnd = 0;
        for (int x = 0; x < size.X - 1; x++)
        {
            bool columnEmpty = true;
            for (int y = 0; y < size.Y - 1; y++)
            {
                if (img.GetPixel(x, y).A > 0.1f)
                {
                    columnEmpty = false;
                    break;
                }
            }

            if (columnEmpty)
            {
                
            }
        }
    }

    public bool FilterUrl(string url)
    {
        return true;
    }

    public string GetName()
    {
        return "single image filter";
    }
}

*/
