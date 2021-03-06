﻿using System.Security.Claims;
using Kros.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sample.AspNetCoreWebApi.Authorization;
using Sample.AspNetCoreWebApi.Filters;
using Sample.AspNetCoreWebApi.Models;
using Sample.AspNetCoreWebApi.ViewModels;

namespace Sample.AspNetCoreWebApi.Controllers
{
    /// <summary>
    /// Controller for login.
    /// </summary>
    [Route("token")]
    [AllowAnonymous]
    public class TokenController : ControllerBase
    {

        private readonly AuthenticationOption _params;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="optionsAccessor">Jwt settings.</param>
        /// <param name="userRepository">Repository for obtaining users.</param>
        public TokenController(IOptions<AuthenticationOption> optionsAccessor, IUserRepository userRepository)
        {
            _userRepository = Check.NotNull(userRepository, nameof(userRepository));
            _params = optionsAccessor.Value;
        }

        /// <summary>
        /// Creating authorization token.
        /// </summary>
        /// <param name="user">User for authentification.</param>
        /// <response code="401">When name or password are incorrect..</response>
        [HttpPost]
        [ModelStateValidationFilter]
        public IActionResult Create([FromBody] UserViewModel user)
        {
            var model = _userRepository.GetByEmail(user.Email);

            if (model == null)
            {
                return Unauthorized();
            }

            var password = new PasswordHasher(_params.Salt).EncryptPassword(user.Password);

            if (model.PasswordHash != password)
            {
                return Unauthorized();
            }

            JwtTokenBuilder builder = new JwtTokenBuilder()
                .AddSecurityKey(JwtToken.GetSecret(_params.Key))
                .AddSubject(_params.Subject)
                .AddIssuer(_params.Issuer)
                .AddAudience(_params.Audience)
                .AddClaim(ClaimTypes.Sid, model.Id.ToString())
                .AddExpiry(_params.ExpirationTime);

            if (model.IsAdmin)
            {
                builder.AddClaim(_params.AdminClaimName, model.Id.ToString());
            }

            return Ok(builder.Build().Value);
        }
    }
}