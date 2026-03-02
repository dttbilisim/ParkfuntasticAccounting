using System;

namespace ecommerce.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class PermissionAttribute : Attribute
    {
        public string MenuPath { get; }
        public string PermissionType { get; }

        public PermissionAttribute(string menuPath, string permissionType)
        {
            MenuPath = menuPath;
            PermissionType = permissionType;
        }
    }
}
