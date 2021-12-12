using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SohatNotebook.Configuration.Messages
{
    public static class ErrorMessages
    {
        public static class Generic
        {
            public static string NotFound = "Resource not found";
            public static string InvalidRequest = "Invalid request";
            public static string BadRequest = "Bad request";
            public static string InvalidPayload = "Invalid payload";
            public static string UnableToProcess = "Unable to process request";
            public static string SomethingWentWrong = "Something went wrong, please try again later";
        }
        public static class Profile
        {
            public static string NotFound = "User not found";

        }

        public static class User
        {
            public static string NotFound = "User not found";

        }
    }
}