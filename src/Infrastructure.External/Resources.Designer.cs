﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Infrastructure.External {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Infrastructure.External.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to The {0} was not found for {1} in the state of the provider.
        /// </summary>
        internal static string BillingProvider_PropertyNotFound {
            get {
                return ResourceManager.GetString("BillingProvider_PropertyNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The ProviderName does not match this provider.
        /// </summary>
        internal static string BillingProvider_ProviderNameNotMatch {
            get {
                return ResourceManager.GetString("BillingProvider_ProviderNameNotMatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot cancel a subscription for a customer that is not immediate or scheduled to cancel in the past.
        /// </summary>
        internal static string ChargebeeHttpServiceClient_Cancel_ScheduleInvalid {
            get {
                return ResourceManager.GetString("ChargebeeHttpServiceClient_Cancel_ScheduleInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The CustomerId is missing.
        /// </summary>
        internal static string ChargebeeHttpServiceClient_InvalidCustomerId {
            get {
                return ResourceManager.GetString("ChargebeeHttpServiceClient_InvalidCustomerId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The PlanId is missing.
        /// </summary>
        internal static string ChargebeeHttpServiceClient_InvalidPlanId {
            get {
                return ResourceManager.GetString("ChargebeeHttpServiceClient_InvalidPlanId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Subscriber is missing.
        /// </summary>
        internal static string ChargebeeHttpServiceClient_InvalidSubscriber {
            get {
                return ResourceManager.GetString("ChargebeeHttpServiceClient_InvalidSubscriber", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The SubscriptionId is missing.
        /// </summary>
        internal static string ChargebeeHttpServiceClient_InvalidSubscriptionId {
            get {
                return ResourceManager.GetString("ChargebeeHttpServiceClient_InvalidSubscriptionId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot create a subscription for a customer that is not immediate or scheduled to start in the past.
        /// </summary>
        internal static string ChargebeeHttpServiceClient_Subscribe_ScheduleInvalid {
            get {
                return ResourceManager.GetString("ChargebeeHttpServiceClient_Subscribe_ScheduleInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A subscription with ID: {0}, does not exist in Chargebee.
        /// </summary>
        internal static string ChargebeeHttpServiceClient_SubscriptionNotFound {
            get {
                return ResourceManager.GetString("ChargebeeHttpServiceClient_SubscriptionNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot transfer a subscription to another buyer without the buyer information.
        /// </summary>
        internal static string ChargebeeHttpServiceClient_Transfer_BuyerInvalid {
            get {
                return ResourceManager.GetString("ChargebeeHttpServiceClient_Transfer_BuyerInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The feature &apos;{0}&apos; has not be defined in Flagsmith.
        /// </summary>
        internal static string FlagsmithHttpServiceClient_UnknownFeature {
            get {
                return ResourceManager.GetString("FlagsmithHttpServiceClient_UnknownFeature", resourceCulture);
            }
        }
    }
}
