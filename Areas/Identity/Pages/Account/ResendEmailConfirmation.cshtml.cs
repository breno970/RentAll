// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using aluguel.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace aluguel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, IConfiguration configuration)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        [EnableRateLimiting("account-email")]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrWhiteSpace(_configuration["SendGridKey"]) || string.IsNullOrWhiteSpace(_configuration["EmailFromAddress"]))
            {
                ModelState.AddModelError(string.Empty, "O envio de e-mails está temporariamente indisponível. Contacta o suporte.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null || await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Success"] = "Se existir uma conta por confirmar, receberás instruções por email.";
                return RedirectToPage("./Login");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = userId, code = code },
                protocol: Request.Scheme);
            try
            {
                await _emailSender.SendEmailAsync(Input.Email, "Confirma o teu email na RentAll",
                    $"<p>Para confirmares o teu email e acederes à RentAll, clica abaixo.</p><p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='display:inline-block;background:#135c47;color:#fff;padding:12px 18px;border-radius:6px;text-decoration:none;font-weight:bold'>Confirmar email</a></p>");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "O email não pôde ser enviado. Tenta novamente mais tarde.");
                return Page();
            }
            TempData["Success"] = "Se existir uma conta por confirmar, receberás instruções por email.";
            return RedirectToPage("./Login");
        }
    }
}
