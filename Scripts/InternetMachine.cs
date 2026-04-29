using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

public partial class InternetMachine : Node
{
    const string HTML_REGEX_PREFIX = "mediaurl=";

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
    private ImageFilter[] filters;
    
    private int imagesRemaining;
    private bool resultGiven;
    private bool isBusy = false;

    public void RequestImage(string query, Action<Image, string> onCompleted, Action onFailure, ImageFilter[] filters)
    {
        CancelAllRequests();

        string queryCompleted = $"{query} photo png";
        string queryEscaped = System.Net.WebUtility.UrlEncode(queryCompleted);

        this.onCompleted = onCompleted;
        this.onFailure = onFailure;
        this.filters = filters;
        isBusy = true;

        string fullQuery = $"https://www.bing.com/images/search?q={queryEscaped}&qft=%20filterui%3Aphoto-transparent";

        GD.Print(fullQuery);
        DoRequest(fullQuery, 3, HandleSearchPageResult, () => { onFailure(); });
    }

    private void HandleSearchPageResult(byte[] body)
    {
        string html = System.Text.Encoding.UTF8.GetString(body); // A bunch of html nonsense
        Regex r = new Regex(HTML_REGEX_PREFIX + "([^&]*)");
        
        Match[] matches = r.Matches(html).Distinct(new MatchComparer()).ToArray();
        imagesRemaining = matches.Length; // Probably wil a lot of duplicates
        resultGiven = false;

        GD.Print("Result count:", imagesRemaining);

        Random.Shared.Shuffle(matches);

        FetchImage(matches, 0);
    }

    private void FetchImage(Match[] urls, int index)
    {
        if (index >= urls.Length) { return; }

        Match m = urls[index];

        string uglyLink = m.Value;
        string cleanedLink = cleanUrl(uglyLink);

        bool ok = true;

        foreach (ImageFilter filter in filters)
        {
            if (!filter.FilterUrl(cleanedLink))
            {
                GD.Print("Url rejected by ", filter.GetName(), ": ", cleanedLink);
                ok = false;
                continue;
            }
        }

        if (!cleanedLink.Contains(".png"))
        {
            GD.Print("Url rejected because not a PNG: ", cleanedLink);
            ok = false;
        }

        if (ok)
        {
            DoRequest(cleanedLink, 2, 
                (body) => {
                    bool ok = HandleImageResult(body, cleanedLink);

                    if (!ok)
                        FetchImage(urls, index + 1);
                }, 
                () => {
                    HandleImageFailure(cleanedLink);
                    FetchImage(urls, index + 1);
                }
            );
        }
        else
        {
            FetchImage(urls, index + 1);
        }
    }

    private bool HandleImageResult(byte[] body, string link)
    {
        Image img;

        try
        {
            FileAccess f = FileAccess.Open($"user://tmp{body.GetHashCode()}.png", FileAccess.ModeFlags.Write);
            f.StoreBuffer(body);
            f.Close();

            img = Image.LoadFromFile(f.GetPath());

            f.Close();
            DirAccess.RemoveAbsolute(f.GetPathAbsolute());

            foreach (ImageFilter filter in filters)
            {
                if (!filter.FilterImage(img))
                {
                    GD.Print("Image rejected by ", filter.GetName(), ": ", link);
                    HandleImageFailure(link);
                    return false;
                }
            }

            CancelAllRequests();
        }
        catch (Exception e)
        {
            GD.Print("error while loading image: ", e);
            HandleImageFailure(link);
            return false;
        }

        GD.Print("Image successful: ", link);

        if (!resultGiven)
        {
            resultGiven = true;
            onCompleted(img, link);
            isBusy = false;
            return true;
        }

        return false;
    }

    private void HandleImageFailure(string link)
    {
        GD.Print("Image failed: ", link);

        imagesRemaining -= 1;
        if (imagesRemaining > 0) return;
        onFailure();
        isBusy = false;
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


    private void DoRequest(string url, int maxRetry, Action<byte[]> onOk, Action onFailed)
    {
        HttpRequest rec = new HttpRequest();
        AddChild(rec); 

        rec.RequestCompleted += (result, responseCode, headers, body) =>
        {
            rec.QueueFree();

            if (result == (long)HttpRequest.Result.Success && responseCode == 200)
            {
                onOk(body);
            }
            else
            {
                if (result != (long)HttpRequest.Result.Success)
                {
                    GD.Print("Invalid result code ", result, " for: ", url);
                }
                else
                {
                    GD.Print("Invalid response code ", responseCode, " for: ", url);
                }

                if (maxRetry > 0)
                {
                    DoRequest(url, maxRetry - 1, onOk, onFailed);
                }
                else
                {
                    onFailed();
                }
            }
        };

        rec.Timeout = requestTimeout;
        rec.Request(url, customHeaders: [
            "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 Edg/147.0.3912.86",
            "Accept-Language: en-US,en"
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
