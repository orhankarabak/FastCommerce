﻿using FastCommerce.DAL;
using FastCommerce.Entities.Entities;
using FastCommerce.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utility;

namespace FastCommerce.Business.UserManager
{
    public class UserManager : IUserManager
    {
        private readonly dbContext _context;
        public UserManager(dbContext context)
        {
            _context = context;
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
            User fetchedUser = _context.Users.Where(w => (w.Username == login.Username) || (w.Email == login.Email)).SingleOrDefault();
            if (fetchedUser != null)
                login.LoggedIn = (fetchedUser.Password == Cryptography.Encrypt(login.Password));
            return login;
        }

        public Register Register(Register register)
        {
            _context.Users.AddAsync(register);
            SetupActivation(register.UserID);
            register.SuccessfullyRegistered = true;
            return register;
        }

        private void SetupActivation(int UserID)
        {

            UsersActivation usersActivation = new UsersActivation();
            usersActivation.user.UserID = UserID;
            usersActivation.startTime = DateTime.Now;
            usersActivation.activetioncode = GenerateActivationCode();
            _context.UsersActivations.Add(usersActivation);
            _context.SaveChangesAsync();
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
    }

    public interface IUserManager
    {
        public Login Login(Login login);
        public Register Register(Register register);
    }
}
