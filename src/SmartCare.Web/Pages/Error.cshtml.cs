using Microsoft.AspNetCore.Mvc;

namespace SmartCare.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : AppPageModel
{
    public void OnGet() { }
}
