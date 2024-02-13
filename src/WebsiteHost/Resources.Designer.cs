﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebsiteHost {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WebsiteHost.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to The &apos;EventName&apos; was either missing or invalid.
        /// </summary>
        internal static string AnyRecordingEventNameValidator_InvalidEventName {
            get {
                return ResourceManager.GetString("AnyRecordingEventNameValidator_InvalidEventName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;AuthCode&apos; is either missing or invalid.
        /// </summary>
        internal static string AuthenticateRequestValidator_InvalidAuthCode {
            get {
                return ResourceManager.GetString("AuthenticateRequestValidator_InvalidAuthCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;Password&apos; is either missing or invalid.
        /// </summary>
        internal static string AuthenticateRequestValidator_InvalidPassword {
            get {
                return ResourceManager.GetString("AuthenticateRequestValidator_InvalidPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;Provider&apos; is either missing or invalid.
        /// </summary>
        internal static string AuthenticateRequestValidator_InvalidProvider {
            get {
                return ResourceManager.GetString("AuthenticateRequestValidator_InvalidProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;Username&apos; is either missing or invalid.
        /// </summary>
        internal static string AuthenticateRequestValidator_InvalidUsername {
            get {
                return ResourceManager.GetString("AuthenticateRequestValidator_InvalidUsername", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;Name&apos; is either missing or invalid.
        /// </summary>
        internal static string GetFeatureFlagForCallerRequestValidator_InvalidName {
            get {
                return ResourceManager.GetString("GetFeatureFlagForCallerRequestValidator_InvalidName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file &apos;{0}&apos; cannot be found in the directory {rootPath}.  Please make sure you have pre-built the JS application by running `npm run build`.
        /// </summary>
        internal static string HomeController_IndexPageNotBuilt {
            get {
                return ResourceManager.GetString("HomeController_IndexPageNotBuilt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;Message&apos; was either missing or invalid.
        /// </summary>
        internal static string RecordCrashRequestValidator_InvalidMessage {
            get {
                return ResourceManager.GetString("RecordCrashRequestValidator_InvalidMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Javascript application crashed, with message: {0}.
        /// </summary>
        internal static string RecordingApplication_RecordCrash_ExceptionMessage {
            get {
                return ResourceManager.GetString("RecordingApplication_RecordCrash_ExceptionMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;Path&apos; was either missing or invalid.
        /// </summary>
        internal static string RecordPageViewRequestValidator_InvalidPath {
            get {
                return ResourceManager.GetString("RecordPageViewRequestValidator_InvalidPath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An &apos;Level&apos; was either missing or invalid.
        /// </summary>
        internal static string RecordTraceRequestValidator_InvalidLevel {
            get {
                return ResourceManager.GetString("RecordTraceRequestValidator_InvalidLevel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An &apos;Argument&apos; was either missing or invalid.
        /// </summary>
        internal static string RecordTraceRequestValidator_InvalidMessageArgument {
            get {
                return ResourceManager.GetString("RecordTraceRequestValidator_InvalidMessageArgument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;MessageTemplate&apos; was either missing or invalid.
        /// </summary>
        internal static string RecordTraceRequestValidator_InvalidMessageTemplate {
            get {
                return ResourceManager.GetString("RecordTraceRequestValidator_InvalidMessageTemplate", resourceCulture);
            }
        }
    }
}
