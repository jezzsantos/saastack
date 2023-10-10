﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Tools.Analyzers.Core {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Tools.Analyzers.Core.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to This type should have a &lt;summary&gt; to describe what it designed to do..
        /// </summary>
        internal static string SAS001Description {
            get {
                return ResourceManager.GetString("SAS001Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type &apos;{0}&apos; requires a documentation &lt;summary/&gt;.
        /// </summary>
        internal static string SAS001MessageFormat {
            get {
                return ResourceManager.GetString("SAS001MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing documentation.
        /// </summary>
        internal static string SAS001Title {
            get {
                return ResourceManager.GetString("SAS001Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This method should have a &lt;summary&gt; to describe what it designed to do..
        /// </summary>
        internal static string SAS002Description {
            get {
                return ResourceManager.GetString("SAS002Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Extension method &apos;{0}&apos; requires a documentation &lt;summary/&gt;.
        /// </summary>
        internal static string SAS002MessageFormat {
            get {
                return ResourceManager.GetString("SAS002MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing documentation.
        /// </summary>
        internal static string SAS002Title {
            get {
                return ResourceManager.GetString("SAS002Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This method should return a Result type..
        /// </summary>
        internal static string SAS010Description {
            get {
                return ResourceManager.GetString("SAS010Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to If method &apos;{0}&apos; is supposed to be a service operation, then it should return one of these possible types: &apos;{1}&apos;.
        /// </summary>
        internal static string SAS010MessageFormat {
            get {
                return ResourceManager.GetString("SAS010MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrong return type.
        /// </summary>
        internal static string SAS010Title {
            get {
                return ResourceManager.GetString("SAS010Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This service operation should have at least one parameter, and that parameter should be derived from: &apos;IWebRequest&lt;TResponse&gt;&apos;..
        /// </summary>
        internal static string SAS011Description {
            get {
                return ResourceManager.GetString("SAS011Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service operation &apos;{0}&apos; should have at least one parameter of a type derived from; &apos;IWebRequest&lt;TResponse&gt;&apos;.
        /// </summary>
        internal static string SAS011MessageFormat {
            get {
                return ResourceManager.GetString("SAS011MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing first parameter or wrong parameter type.
        /// </summary>
        internal static string SAS011Title {
            get {
                return ResourceManager.GetString("SAS011Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This service operation can only have a &apos;CancellationToken&apos; as its second parameter..
        /// </summary>
        internal static string SAS012Description {
            get {
                return ResourceManager.GetString("SAS012Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service operation &apos;{0}&apos; can only have a &apos;CancellationToken&apos; as its second parameter.
        /// </summary>
        internal static string SAS012MessageFormat {
            get {
                return ResourceManager.GetString("SAS012MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrong second parameter type.
        /// </summary>
        internal static string SAS012Title {
            get {
                return ResourceManager.GetString("SAS012Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This service operation should be declared with a &apos;WebApiRouteAttribute&apos; on it..
        /// </summary>
        internal static string SAS013Description {
            get {
                return ResourceManager.GetString("SAS013Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service operation &apos;{0}&apos; should have a &apos;WebApiRouteAttribute&apos;.
        /// </summary>
        internal static string SAS013MessageFormat {
            get {
                return ResourceManager.GetString("SAS013MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing &apos;WebApiRouteAttribute&apos;.
        /// </summary>
        internal static string SAS013Title {
            get {
                return ResourceManager.GetString("SAS013Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This service operation has a route declared on it that is different from other service operations in this class..
        /// </summary>
        internal static string SAS014Description {
            get {
                return ResourceManager.GetString("SAS014Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service operation &apos;{0}&apos; is required to have the same route path as other service operations in this class.
        /// </summary>
        internal static string SAS014MessageFormat {
            get {
                return ResourceManager.GetString("SAS014MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrong route group.
        /// </summary>
        internal static string SAS014Title {
            get {
                return ResourceManager.GetString("SAS014Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This service operation has the same request type as another service operation in this class..
        /// </summary>
        internal static string SAS015Description {
            get {
                return ResourceManager.GetString("SAS015Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service operation &apos;{0}&apos; uses the same type for its first parameter as does another service operation in this class. They must use different request types.
        /// </summary>
        internal static string SAS015MessageFormat {
            get {
                return ResourceManager.GetString("SAS015MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicate request type.
        /// </summary>
        internal static string SAS015Title {
            get {
                return ResourceManager.GetString("SAS015Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This service operation should return an appropriate Result type for the operation..
        /// </summary>
        internal static string SAS016Description {
            get {
                return ResourceManager.GetString("SAS016Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service operation &apos;{0}&apos; is defined as a &apos;{1}&apos; operation, and can only return one of these types: &apos;{2}&apos;.
        /// </summary>
        internal static string SAS016MessageFormat {
            get {
                return ResourceManager.GetString("SAS016MessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected return type for operation.
        /// </summary>
        internal static string SAS016Title {
            get {
                return ResourceManager.GetString("SAS016Title", resourceCulture);
            }
        }
    }
}
