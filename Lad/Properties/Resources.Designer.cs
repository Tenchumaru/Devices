﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Lad.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Lad.Properties.Resources", typeof(Resources).Assembly);
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
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to L{1}i{0}_:
        ///accept_ = {2};
        ///yy.Save();
        ///M{1}i{0}_:
        ///{3}.
        /// </summary>
        internal static string AcceptingFinalState {
            get {
                return ResourceManager.GetString("AcceptingFinalState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to L{1}i{0}_:
        ///accept_ = {2};
        ///yy.Save();
        ///M{1}i{0}_:
        ///switch(ch_ = yy.Get())
        ///{{
        ///{3}
        ///}}.
        /// </summary>
        internal static string AcceptingState {
            get {
                return ResourceManager.GetString("AcceptingState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to L{1}i{0}_:
        ///M{1}i{0}_:
        ///{2}.
        /// </summary>
        internal static string NonAcceptingFinalState {
            get {
                return ResourceManager.GetString("NonAcceptingFinalState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to L{1}i{0}_:
        ///M{1}i{0}_:
        ///switch(ch_ = yy.Get())
        ///{{
        ///{2}
        ///}}.
        /// </summary>
        internal static string NonAcceptingState {
            get {
                return ResourceManager.GetString("NonAcceptingState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to int ch_, accept_ = -1;
        ///goto M0i{0}_;
        ///done_:
        ///yy.Restore();
        ///#pragma warning disable 162
        ///switch(accept_)
        ///{{
        ///{1}
        ///case -1: goto X{0}_;
        ///}}
        ///accept_ = -1;
        ///#pragma warning restore 162.
        /// </summary>
        internal static string Prologue {
            get {
                return ResourceManager.GetString("Prologue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.IO;
        ///
        ///	class Scanner // using directives, namespace, and class declaration $
        ///	{
        ///		private class YY
        ///		{
        ///			private TextReader reader;
        ///			private int marker, position;
        ///			private StringBuilder buffer = new StringBuilder();
        ///			private string tokenValue;
        ///			private int scanValue;
        ///#if TRACKING_LINE_NUMBER
        ///			private int lineNumber;
        ///#endif
        ///#if USES_BOL
        ///			private bool atBol = true;
        ///#endif
        ///
        ///			public YY(TextReader r [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Skeleton {
            get {
                return ResourceManager.GetString("Skeleton", resourceCulture);
            }
        }
    }
}
