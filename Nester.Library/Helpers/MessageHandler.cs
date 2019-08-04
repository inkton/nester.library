/*
    Copyright (c) 2017 Inkton.

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the Software
    is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Resources;
using Xamarin.Forms;
using Inkton.Nest.Cloud;

namespace Inkton.Nester.Helpers
{
    public static class MessageHandler
    {
        public static string GetMessage(string id, params object[] args)
        {
            ResourceManager resmgr = (Application.Current as INesterClient)
                .GetResourceManager();
            string message = resmgr.GetString(id,
                System.Globalization.CultureInfo.CurrentUICulture);
            return string.Format(message, args);
        }

        public static string GetMessage<T>(Result<T> result, params object[] args)
        {
            ResourceManager resmgr = (Application.Current as INesterClient)
                .GetResourceManager();
            string message = resmgr.GetString(result.Text,
                System.Globalization.CultureInfo.CurrentUICulture);
            return string.Format(message, args);
        }

        public static void ThrowMessage(string id,
            Severity severity = Severity.SeverityInfo, params object[] args)
        {
            string message = GetMessage(id, args);
            ILogService logService = DependencyService.Get<ILogService>();
            logService?.Trace(message, Environment.StackTrace, severity);
            throw new Exception(message);
        }

        public static void ThrowMessage<T>(Result<T> result,
            Severity severity = Severity.SeverityInfo,  params object[] args)
        {
            string message = string.Empty;

            if (!string.IsNullOrEmpty(result.Text))
            {
                try
                {
                    message = GetMessage(result.Text, args);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceError(e.Message);
                }

                if (message == string.Empty)
                {
                    if (result.Code >= 0)
                    {
                        message = "Sucess!";
                    }
                    else
                    {
                        message = "Oops! somthing went wrong!";
                    }
                }
                else
                {
                    if (result.Text == "NEST_RESULT_HTTP_ERROR")
                    {
                        message += " - " + result.HttpStatus.ToString();
                    }
                }
            }

            if (!string.IsNullOrEmpty(result.Notes))
            {
                message += "\n" + result.Notes;
            }

            ILogService logService = DependencyService.Get<ILogService>();
            logService?.Trace(message, Environment.StackTrace, severity);

            throw new Exception(message);
        }
    }
}
