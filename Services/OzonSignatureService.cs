// ApiOzon/Services/OzonSignatureService.cs
using System.Security.Cryptography;
using System.Text;

namespace ApiOzon.Services;

public interface IOzonSignatureService
{
    string GenerateSignature(string httpMethod, string path, string requestBody, string secretKey);
}

public class OzonSignatureService : IOzonSignatureService
{
    public string GenerateSignature(string httpMethod, string path, string requestBody, string secretKey)
    {
        // 1. Собираем строку для подписи
        var signingString = $"{httpMethod}\n{path}\n{requestBody}";
        
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signingString));
        
        // 2. Конвертируем в Base64 (важно для Acquiring API!)
        return Convert.ToBase64String(hash);
    }
}