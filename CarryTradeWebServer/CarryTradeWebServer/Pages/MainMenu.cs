using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace coderush.Pages
{
    public static class MainMenu
    {
        public static class User
        {
            public const string PageName = "User";
            public const string RoleName = "User";
            public const string Path = "/UserRole/Index";
            public const string ControllerName = "UserRole";
            public const string ActionName = "Index";
        }

        public static class Membership
        {
            public const string PageName = "Role";//"Membership";
            public const string RoleName = "Membership";
            public const string Path = "/UserRole/Membership";
            public const string ControllerName = "UserRole";
            public const string ActionName = "Membership";
        }

        public static class ChangePassword
        {
            public const string PageName = "Change Password";
            public const string RoleName = "Change Password";
            public const string Path = "/UserRole/ChangePassword";
            public const string ControllerName = "UserRole";
            public const string ActionName = "ChangePassword";
        }

        public static class Role
        {
            public const string PageName = "Permission";//"Role";
            public const string RoleName = "Role";
            public const string Path = "/UserRole/Role";
            public const string ControllerName = "UserRole";
            public const string ActionName = "Role";
        }

        public static class ChangeRole
        {
            public const string PageName = "Edit Permission";//"Change Role";
            public const string RoleName = "Change Role";
            public const string Path = "/UserRole/ChangeRole";
            public const string ControllerName = "UserRole";
            public const string ActionName = "ChangeRole";
        }

        public static class Dashboard
        {
            public const string PageName = "Dashboard Main";
            public const string RoleName = "Dashboard Main";
            public const string Path = "/Dashboard/Index";
            public const string ControllerName = "Dashboard";
            public const string ActionName = "Index";
        }

        public static class TerminalManage
        {
            public const string PageName = "TerminalManage";
            public const string RoleName = "TerminalManage";
            public const string Path = "/Terminal/Index";
            public const string ControllerName = "Terminal";
            public const string ActionName = "Index";
        }
    }
}
