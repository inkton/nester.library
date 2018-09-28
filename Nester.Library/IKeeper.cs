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

using System.Resources;
using System.Threading.Tasks;
using Inkton.Nester.ViewModels;
using Inkton.Nest.Model;
using Inkton.Nester.Cloud;
using Inkton.Nester.Storage;

namespace Inkton.Nester
{
    public interface IKeeper : IClientResources
    {
        /// <summary>
        /// The nest.yt platform user
        /// </summary>
        User User
        {
            get; set;
        }

        /// <summary>
        /// Holds, the authentication, 
        /// app and payment contexts
        /// <summary>
        BaseViewModels ViewModels
        {
            get;
        }

        /// <summary>
        /// The current application
        /// <summary>
        AppViewModel Target
        {
            get;
        }

        /// <summary>
        /// A connection to the nest.yt platform 
        /// to draw app meta data
        /// <summary>
        NesterService Service
        {
            get;
        }

        /// <summary>
        /// Reset the view optionally with app
        /// <summary>
        void ResetView(AppViewModel appModel = null);
    }
}
