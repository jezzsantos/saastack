﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SubscriptionsDomain {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SubscriptionsDomain.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Canceling the plan failed, reason: {0}.
        /// </summary>
        internal static string SubscriptionRoot_CancelSubscription_FailedWithReason {
            get {
                return ResourceManager.GetString("SubscriptionRoot_CancelSubscription_FailedWithReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This subscription cannot be canceled when it is not active.
        /// </summary>
        internal static string SubscriptionRoot_CancelSubscription_NotCancellable {
            get {
                return ResourceManager.GetString("SubscriptionRoot_CancelSubscription_NotCancellable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only a service account can cancel the subscription.
        /// </summary>
        internal static string SubscriptionRoot_CancelSubscriptionFromProvider_NotAuthorized {
            get {
                return ResourceManager.GetString("SubscriptionRoot_CancelSubscriptionFromProvider_NotAuthorized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This subscription cannot be terminated in its current state.
        /// </summary>
        internal static string SubscriptionRoot_CannotBeUnsubscribed {
            get {
                return ResourceManager.GetString("SubscriptionRoot_CannotBeUnsubscribed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only a service account can change the payment method.
        /// </summary>
        internal static string SubscriptionRoot_ChangeBuyerPaymentMethodFromProvider_NotAuthorized {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ChangeBuyerPaymentMethodFromProvider_NotAuthorized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Changing the plan failed, reason: {0}.
        /// </summary>
        internal static string SubscriptionRoot_ChangePlan_FailedWithReason {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ChangePlan_FailedWithReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Until the buyer of this subscription has a valid payment method, that cannot change the plan.
        /// </summary>
        internal static string SubscriptionRoot_ChangePlan_InvalidPaymentMethod {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ChangePlan_InvalidPaymentMethod", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Until the buyer has canceled the subscription, another user cannot gain authority for the subscription.
        /// </summary>
        internal static string SubscriptionRoot_ChangePlan_NotClaimable {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ChangePlan_NotClaimable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only a service account can change the provider.
        /// </summary>
        internal static string SubscriptionRoot_ChangeProvider_NotAuthorized {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ChangeProvider_NotAuthorized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only a service account can change the subscription plan.
        /// </summary>
        internal static string SubscriptionRoot_ChangeSubscriptionPlanFromProvider_NotAuthorized {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ChangeSubscriptionPlanFromProvider_NotAuthorized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Can only delete the subscription for this owning entity.
        /// </summary>
        internal static string SubscriptionRoot_DeleteSubscription_NotOwningEntityId {
            get {
                return ResourceManager.GetString("SubscriptionRoot_DeleteSubscription_NotOwningEntityId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only a service account can delete the subscription.
        /// </summary>
        internal static string SubscriptionRoot_DeleteSubscriptionFromProvider_NotAuthorized {
            get {
                return ResourceManager.GetString("SubscriptionRoot_DeleteSubscriptionFromProvider_NotAuthorized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider must be same as that of the currently installed provider.
        /// </summary>
        internal static string SubscriptionRoot_InstalledProviderMismatch {
            get {
                return ResourceManager.GetString("SubscriptionRoot_InstalledProviderMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Subscription must have a valid buyer ID.
        /// </summary>
        internal static string SubscriptionRoot_NoBuyer {
            get {
                return ResourceManager.GetString("SubscriptionRoot_NoBuyer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Subscription must have a valid owning entity ID.
        /// </summary>
        internal static string SubscriptionRoot_NoOwningEntity {
            get {
                return ResourceManager.GetString("SubscriptionRoot_NoOwningEntity", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider has not yet been set.
        /// </summary>
        internal static string SubscriptionRoot_NoProvider {
            get {
                return ResourceManager.GetString("SubscriptionRoot_NoProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only the buyer of this subscription can perform this action.
        /// </summary>
        internal static string SubscriptionRoot_NotBuyer {
            get {
                return ResourceManager.GetString("SubscriptionRoot_NotBuyer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider must be same as that of the current provider.
        /// </summary>
        internal static string SubscriptionRoot_ProviderMismatch {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ProviderMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Until the provider has been initialized, this action cannot be performed.
        /// </summary>
        internal static string SubscriptionRoot_ProviderNotInitialized {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ProviderNotInitialized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only a service account can delete the buyer.
        /// </summary>
        internal static string SubscriptionRoot_RestoreBuyerAfterDeletedFromProvider_NotAuthorized {
            get {
                return ResourceManager.GetString("SubscriptionRoot_RestoreBuyerAfterDeletedFromProvider_NotAuthorized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider cannot be changed to the same provider again.
        /// </summary>
        internal static string SubscriptionRoot_SameProvider {
            get {
                return ResourceManager.GetString("SubscriptionRoot_SameProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Transferring the subscription failed, reason: {0}.
        /// </summary>
        internal static string SubscriptionRoot_TransferSubscription_FailedWithReason {
            get {
                return ResourceManager.GetString("SubscriptionRoot_TransferSubscription_FailedWithReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Until the user being transferred the subscription has a valid payment method, they cannot gain authority for the subscription.
        /// </summary>
        internal static string SubscriptionRoot_TransferSubscription_InvalidPaymentMethod {
            get {
                return ResourceManager.GetString("SubscriptionRoot_TransferSubscription_InvalidPaymentMethod", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unsubscribing from the subscription failed, reason: {0}.
        /// </summary>
        internal static string SubscriptionRoot_UnsubscribeSubscription_FailedWithReason {
            get {
                return ResourceManager.GetString("SubscriptionRoot_UnsubscribeSubscription_FailedWithReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Viewing the subscription failed, reason: {0}.
        /// </summary>
        internal static string SubscriptionRoot_ViewSubscription_FailedWithReason {
            get {
                return ResourceManager.GetString("SubscriptionRoot_ViewSubscription_FailedWithReason", resourceCulture);
            }
        }
    }
}
