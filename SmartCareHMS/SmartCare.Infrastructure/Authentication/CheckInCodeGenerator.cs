using System.Security.Cryptography;
using System.Text;
using SmartCare.Application.Interfaces.Security;

namespace SmartCare.Infrastructure.Authentication;

public sealed class CheckInCodeGenerator : ICheckInCodeGenerator
{
    public CheckInCode Create(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var code = Convert.ToHexString(hash)[..8].ToUpperInvariant();
        var svg = BuildSvg(hash);

        return new CheckInCode { Code = code, SvgMarkup = svg };
    }

    private static string BuildSvg(byte[] hash)
    {
        const int cells = 21;
        const int cell = 8;
        var size = cells * cell;
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{size}\" height=\"{size}\" viewBox=\"0 0 {size} {size}\" role=\"img\" aria-label=\"Check-in placeholder\">");
        sb.Append($"<rect width=\"{size}\" height=\"{size}\" fill=\"#ffffff\"/>");

        var bit = 0;
        for (var y = 0; y < cells; y++)
        {
            for (var x = 0; x < cells; x++)
            {
                var byteIndex = (bit / 8) % hash.Length;
                var mask = 1 << (bit % 8);
                if ((hash[byteIndex] & mask) != 0)
                    sb.Append($"<rect x=\"{x * cell}\" y=\"{y * cell}\" width=\"{cell}\" height=\"{cell}\" fill=\"#111111\"/>");
                bit++;
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}
