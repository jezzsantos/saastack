﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Domain.Common {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Domain.Common.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Failed to deserialize event &apos;{0}&apos;. The serialized type: &apos;{1}&apos; cannot be found in the current AppDomain. Perhaps the type &apos;{1}&apos; has been renamed or no longer exists in the codebase?.
        /// </summary>
        internal static string ChangeEventMigrator_UnknownType {
            get {
                return ResourceManager.GetString("ChangeEventMigrator_UnknownType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Change events have already been loaded into the aggregate.
        /// </summary>
        internal static string EventingAggregateRootBase_ChangesAlreadyLoaded {
            get {
                return ResourceManager.GetString("EventingAggregateRootBase_ChangesAlreadyLoaded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The entity has no identifier.
        /// </summary>
        internal static string EventingAggregateRootBase_HasNoIdentifier {
            get {
                return ResourceManager.GetString("EventingAggregateRootBase_HasNoIdentifier", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unhandled event {0}.
        /// </summary>
        internal static string EventingEntityBase_HandleUnKnownStateChangedEvent_UnhandledEvent {
            get {
                return ResourceManager.GetString("EventingEntityBase_HandleUnKnownStateChangedEvent_UnhandledEvent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to deserialize event &apos;{0}&apos; as type: &apos;{1}&apos;.
        /// </summary>
        internal static string EventMetadataExtensions_CreateEventFromJson_FailedDeserialization {
            get {
                return ResourceManager.GetString("EventMetadataExtensions_CreateEventFromJson_FailedDeserialization", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The version of this loaded event (&apos;{0}&apos;) was not expected. Expected version &apos;{1}&apos; instead. Perhaps there is a missing event or this event was replayed out of order?.
        /// </summary>
        internal static string EventStream_OutOfOrderChange {
            get {
                return ResourceManager.GetString("EventStream_OutOfOrderChange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to First version cannot be 0.
        /// </summary>
        internal static string EventStream_ZeroFirstVersion {
            get {
                return ResourceManager.GetString("EventStream_ZeroFirstVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Last version cannot be 0.
        /// </summary>
        internal static string EventStream_ZeroLastVersion {
            get {
                return ResourceManager.GetString("EventStream_ZeroLastVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The name of the topic is invalid.
        /// </summary>
        internal static string MessageBusTopicMessageIdFactory_InvalidTopicName {
            get {
                return ResourceManager.GetString("MessageBusTopicMessageIdFactory_InvalidTopicName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The name of the queue is invalid.
        /// </summary>
        internal static string MessageQueueMessageIdFactory_InvalidQueueName {
            get {
                return ResourceManager.GetString("MessageQueueMessageIdFactory_InvalidQueueName", resourceCulture);
            }
        }
    }
}
