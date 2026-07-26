using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Subsonic.Handlers {
    public static class SubsonicUserHandlers {
        public static void GetUser(SubsonicRequest req, HttpContextProcessor processor, User user) {
            string username = req.Get("username") ?? user.UserName;

            // Non-admins may only view themselves
            if (!user.HasPermission(Role.Admin) && !String.Equals(username, user.UserName, StringComparison.OrdinalIgnoreCase)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotAuthorized, user.UserName + " is not authorized to view other users");
                return;
            }

            User target = Injection.Get<IUserRepository>().UserForName(username);
            if (target == null || target.UserId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "User not found");
                return;
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.User = SubsonicMapper.UserFromUser(target, MediaFolderIds());
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetUsers(SubsonicRequest req, HttpContextProcessor processor, User user) {
            IList<int> folderIds = MediaFolderIds();
            List<SubsonicUser> users = Injection.Get<IUserRepository>().AllUsers()
                    .Where(u => u.UserId != null)
                    .Select(u => SubsonicMapper.UserFromUser(u, folderIds))
                    .ToList();

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Users = new SubsonicUsers { User = users };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void ChangePassword(SubsonicRequest req, HttpContextProcessor processor, User user) {
            string username = req.Get("username");
            string password = DecodePassword(req.Get("password"));
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameters username and password are missing");
                return;
            }

            // Non-admins may only change their own password
            if (!user.HasPermission(Role.Admin) && !String.Equals(username, user.UserName, StringComparison.OrdinalIgnoreCase)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotAuthorized, user.UserName + " is not authorized to change other users' passwords");
                return;
            }

            User target = Injection.Get<IUserRepository>().UserForName(username);
            if (target == null || target.UserId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "User not found");
                return;
            }

            if (!target.UpdatePassword(password)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "Failed to change password");
                return;
            }

            Injection.Get<SubsonicAuth>().Evict(target.UserName);
            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        public static void CreateUser(SubsonicRequest req, HttpContextProcessor processor, User user) {
            string username = req.Get("username");
            string password = DecodePassword(req.Get("password"));
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameters username and password are missing");
                return;
            }

            Role role = req.GetBool("adminRole", false) ? Role.Admin : Role.User;
            User created = Injection.Get<IUserRepository>().CreateUser(username, password, role, null);
            if (created == null || created.UserId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "User " + username + " already exists");
                return;
            }

            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        public static void UpdateUser(SubsonicRequest req, HttpContextProcessor processor, User user) {
            string username = req.Get("username");
            if (String.IsNullOrEmpty(username)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter username is missing");
                return;
            }

            User target = Injection.Get<IUserRepository>().UserForName(username);
            if (target == null || target.UserId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "User not found");
                return;
            }

            string password = DecodePassword(req.Get("password"));
            if (!String.IsNullOrEmpty(password) && !target.UpdatePassword(password)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "Failed to change password");
                return;
            }

            string adminRole = req.Get("adminRole");
            if (adminRole != null) {
                Role role = req.GetBool("adminRole", false) ? Role.Admin : Role.User;
                if (role != target.Role && !target.UpdateRole(role)) {
                    SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "Failed to change role");
                    return;
                }
            }

            Injection.Get<SubsonicAuth>().Evict(target.UserName);
            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        public static void DeleteUser(SubsonicRequest req, HttpContextProcessor processor, User user) {
            string username = req.Get("username");
            if (String.IsNullOrEmpty(username)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter username is missing");
                return;
            }

            User target = Injection.Get<IUserRepository>().UserForName(username);
            if (target == null || target.UserId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "User not found");
                return;
            }

            if (!target.Delete()) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "Failed to delete user");
                return;
            }

            Injection.Get<SubsonicAuth>().Evict(target.UserName);
            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        private static IList<int> MediaFolderIds() {
            return Injection.Get<IFolderRepository>().MediaFolders()
                   .Where(f => f.FolderId != null)
                   .Select(f => (int)f.FolderId)
                   .ToList();
        }

        // Subsonic passwords may arrive hex-encoded as enc:HEX
        private static string DecodePassword(string password) {
            if (password != null && password.StartsWith("enc:", StringComparison.OrdinalIgnoreCase)) {
                try {
                    return Encoding.UTF8.GetString(Convert.FromHexString(password.Substring(4)));
                } catch (FormatException) {
                    return password;
                }
            }
            return password;
        }
    }
}
