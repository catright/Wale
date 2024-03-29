﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Wale.WPF.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Wale.WPF.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Latest usage and full description are always firstly updated on Github Wiki(https://github.com/catright/Wale/wiki)
        ///Last Update 0.5.7
        ///
        ///
        ///Usage
        ///
        ///Wale tries to adjust all sound generating processes&apos; peak level to Target-Yellow bar- level. You can change Target to your preferred level.
        ///
        ///However, Wale uses windows volume system for now, which means the app only can control volume between 0.0~1.0. Hence, if you set Target to near 1.0, then Wale could not control volume, because all processes&apos; peak levels a [rest of string was truncated]&quot;;.
        /// </summary>
        public static string Help {
            get {
                return ResourceManager.GetString("Help", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Microsoft Public License (Ms-PL)
        ///
        ///Microsoft Public License (Ms-PL)
        ///
        ///This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
        ///1. Definitions
        ///
        ///The terms &quot;reproduce,&quot; &quot;reproduction,&quot; &quot;derivative works,&quot; and &quot;distribution&quot; have the same meaning here as under U.S. copyright law.
        ///
        ///A &quot;contribution&quot; is the original software, or any additions or changes to the software.
        ///
        ///A &quot;contributor&quot; is any person [rest of string was truncated]&quot;;.
        /// </summary>
        public static string LicenseCSCore {
            get {
                return ResourceManager.GetString("LicenseCSCore", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Math.NET Numerics License (MIT/X11)
        ///
        ///Copyright (c) 2002-2015 Math.NET
        ///
        ///Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the &quot;Software&quot;), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
        ///
        ///T [rest of string was truncated]&quot;;.
        /// </summary>
        public static string LicenseMathDotNet {
            get {
                return ResourceManager.GetString("LicenseMathDotNet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This app is distributed with MIT license, but it&apos;s restricted tightly to my own codes.
        ///Icons and 3rd party libraries are NOT included in this license. They are copyrighted on their right holders.
        ///Icons are copyrighted on Hyein, Park.
        ///
        ///
        ///
        ///MIT License
        ///
        ///Copyright (c) 2017-2022 Jongtae, Park (catright)
        ///
        ///Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the &quot;Software&quot;), to deal in the Software without restriction, including  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string LicenseWale {
            get {
                return ResourceManager.GetString("LicenseWale", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
        /// </summary>
        public static System.Drawing.Icon WaleLeftOff {
            get {
                object obj = ResourceManager.GetObject("WaleLeftOff", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
        /// </summary>
        public static System.Drawing.Icon WaleLeftOn {
            get {
                object obj = ResourceManager.GetObject("WaleLeftOn", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
        /// </summary>
        public static System.Drawing.Icon WaleRightOff {
            get {
                object obj = ResourceManager.GetObject("WaleRightOff", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
        /// </summary>
        public static System.Drawing.Icon WaleRightoOn {
            get {
                object obj = ResourceManager.GetObject("WaleRightoOn", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
    }
}
