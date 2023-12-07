using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Karami.Core.Common.ClassExtensions;

public static class IFormFileExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <param name="webHostEnvironment"></param>
    /// <param name="renameFile"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<(string path, string name, string extension)> UploadAsync(this IFormFile file, 
        IWebHostEnvironment webHostEnvironment, bool renameFile = true, CancellationToken cancellationToken = default
    )
    {
        string uploadPath = default;
        var fileExtension = Path.GetExtension(file.FileName);
        var fileName      = renameFile ? Guid.NewGuid().ToString().Replace("-", "") + fileExtension : file.FileName;

        if (file.IsImage())
            uploadPath = Path.Combine($"{webHostEnvironment.WebRootPath}", "Storages", "Images", fileName);
        else if (file.IsVideo())
            uploadPath = Path.Combine($"{webHostEnvironment.WebRootPath}", "Storages", "Videos", fileName);
        
        await using var fileStream = new FileStream(uploadPath , FileMode.Create);
        
        await file.CopyToAsync(fileStream, cancellationToken);
        
        return ( uploadPath , fileName , fileExtension );
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static bool IsImage(this IFormFile file)
    {
        //Check Mime Type
        if
        (
            file.ContentType.ToLower() != "image/jpg"  &&
            file.ContentType.ToLower() != "image/jpeg" &&
            file.ContentType.ToLower() != "image/png"
        )
            return false;

        //Check Extension
        if
        (
            Path.GetExtension(file.FileName).ToLower() != ".jpg"  &&
            Path.GetExtension(file.FileName).ToLower() != ".jpeg" &&
            Path.GetExtension(file.FileName).ToLower() != ".png"
        )
            return false;

        //Check Readable file & Security
        try
        {
            if (!file.OpenReadStream().CanRead)
                return false;

            byte[] buffer = new byte[(int) file.Length];
            file.OpenReadStream().Read(buffer, 0, (int) file.Length);
            string content = Encoding.UTF8.GetString(buffer);
            if (Regex.IsMatch(content, @"<script|<html|<head|<title|<body|<pre|<table|<a\s+href|<img|<plaintext|<cross\-domain\-policy", RegexOptions.IgnoreCase))
                return false;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            file.OpenReadStream().Close();
        }

        return true;
    }
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static bool IsVideo(this IFormFile file)
    {
        //Check Mime Type
        if
        (
            file.ContentType.ToLower() != "video/mp4" &&
            file.ContentType.ToLower() != "video/avi"
        )
            return false;

        //Check Extension
        if
        (
            Path.GetExtension(file.FileName).ToLower() != ".mp4" &&
            Path.GetExtension(file.FileName).ToLower() != ".avi"
        )
            return false;

        //Check Readable file & Security
        try
        {
            if (!file.OpenReadStream().CanRead)
                return false;

            byte[] buffer = new byte[(int) file.Length];
            file.OpenReadStream().Read(buffer, 0, (int) file.Length);
            string content = Encoding.UTF8.GetString(buffer);
            if (Regex.IsMatch(content, @"<script|<html|<head|<title|<body|<pre|<table|<a\s+href|<img|<plaintext|<cross\-domain\-policy", RegexOptions.IgnoreCase))
                return false;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            file.OpenReadStream().Close();
        }

        return true;
    }
}