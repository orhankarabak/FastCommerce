﻿using FastCommerce.DAL;
using FastCommerce.Entities.Entities;
using FastCommerce.Entities.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Utility.Cryptography;
using Utility.MailServices;

namespace FastCommerce.Business.UserManager
{
    public class UserManager : IUserManager
    {
        private readonly TokenModel _tokenManagement;
        private readonly dbContext _context;
        private IEmailService _mailService;
        public UserManager(dbContext context, IEmailService mailService, IOptions<TokenModel> tokenManagement)
        {
            _context = context;
            _mailService = mailService;
    
            _tokenManagement = tokenManagement.Value;
        }

        public User AddUser(User user)
        {
            _context.Users.Add(user);
            return user;
        }



        public void PasiveUser(User user)
        {
            _context.Users.Where(u => u.UserID == user.UserID).FirstOrDefault().Active = false;
        }

        public void UpdatePassword(User user)
        {
            _context.Users.Where(w => w.UserID == user.UserID).SingleOrDefault().Password = Cryptography.Encrypt(user.Password);
        }

        public Login Login(Login login)
        {
            User fetchedUser = _context.Users.Where(w => w.Email == login.Email).SingleOrDefault();
            if (fetchedUser != null)
            {
                login.LoggedIn = (fetchedUser.Password == Cryptography.Encrypt(login.Password));
                if (login.LoggedIn) {
                    IsAuthenticated(login, out string token);
                    login.Token = token;
                }
                    
            }
                
            return login;
        }

        public Register Register(Register register)
        {
            _context.Users.AddAsync(register);
            register.SuccessfullyRegistered = true;
            _context.SaveChanges();
            SetupActivation(register);
            return register;
        }

        private void SetupActivation(Register user)
        {

            UsersActivation usersActivation = new UsersActivation();
            usersActivation.user = user;
            usersActivation.startTime = DateTime.Now;
            usersActivation.activetioncode = GenerateActivationCode();
            _mailService.SetMailBoxes = ConvertUserToMailBoxesArray(user);
            _mailService.SendEmailAsync(EmailType.activationCode);
            _context.UsersActivations.Add(usersActivation);
            _context.SaveChangesAsync();
        }


        private string[] ConvertUserToMailBoxesArray(Register user)
        {
            string[] userNameMail = new string[2];
            userNameMail[0] = user.Name + " " + user.Surname;
            userNameMail[1] = user.Email;
            return userNameMail;
        }

        private bool Activate(int UserID, string Code)
        {
            var activation = _context.UsersActivations.Where(s => s.user.UserID == UserID).FirstOrDefault();
            if (activation != null)
            { return (activation.activetioncode == Code); }
            else { return false; }
        }

        private bool SendActivationEmail(int UserID)
        {
            return true;
        }


        private string GenerateActivationCode()
        {
            Random generator = new Random();
            String code = generator.Next(0, 999999).ToString("D6");
            return code;
        }

        private bool IsAuthenticated(Login request, out string token)
        {
            token = string.Empty;
            var claim = new[]
            {
                new Claim(ClaimTypes.Name, request.Email)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenManagement.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(
                          _tokenManagement.Issuer,
                          _tokenManagement.Audience,
                          claim,
                          expires: DateTime.Now.AddMinutes(_tokenManagement.AccessExpiration),
                          signingCredentials: credentials
                      );

            token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            return true;
        }
    }

    public interface IUserManager
    {
        public Login Login(Login login);
        public Register Register(Register register);
    }
}
