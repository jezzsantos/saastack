## Release 1.0

### New Rules

 Rule ID    | Category            | Severity | Notes                                                                                                     
------------|---------------------|----------|-----------------------------------------------------------------------------------------------------------
 SAASWEB010 | SaaStackWebApi      | Warning  | Methods that are public, should return a Task<T> or just any T, where T is one of the supported results . 
 SAASWEB011 | SaaStackWebApi      | Warning  | These methods must have at least one parameter.                                                           
 SAASWEB012 | SaaStackWebApi      | Warning  | The second parameter can only be a CancellationToken.                                                     
 SAASWEB013 | SaaStackWebApi      | Warning  | These methods must be decorated with a RouteAttribute.                                                    
 SAASWEB014 | SaaStackWebApi      | Warning  | The route (of all these methods in this class) should start with the same path.                           
 SAASWEB015 | SaaStackWebApi      | Warning  | There should be no methods in this class with the same IWebRequest{TResponse}.                            
 SAASWEB016 | SaaStackWebApi      | Warning  | This service operation should return an appropriate Result type for the operation.                        
 SAASWEB017 | SaaStackWebApi      | Warning  | The request type should be declared with a RouteAttribute on it.                                          
 SAASWEB018 | SaaStackWebApi      | Error    | The request type should not be declared with a AuthorizeAttribute on it.                                  
 SAASWEB019 | SaaStackWebApi      | Warning  | The request type should be declared with a AuthorizeAttribute on it.                                      
 SAASWEB020 | SaaStackWebApi      | Warning  | There should be no methods in this class with the same route.                                             
 SAASWEB030 | SaaStackWebApi      | Error    | Request must be public                                                                                    
 SAASWEB031 | SaaStackWebApi      | Error    | Request must be named with "Request" suffix                                                               
 SAASWEB032 | SaaStackWebApi      | Error    | Request must be in namespace "Infrastructure.Web.Api.Operations.Shared"                                   
 SAASWEB033 | SaaStackWebApi      | Error    | Request must have a <see cref="RouteAttribute" />                                                         
 SAASWEB034 | SaaStackWebApi      | Error    | Request must have a parameterless constructor                                                             
 SAASWEB035 | SaaStackWebApi      | Error    | Properties must have public getters and setters                                                           
 SAASWEB036 | SaaStackWebApi      | Error    | Properties should be nullable not Optional{T} for interoperability                                        
 SAASWEB037 | SaaStackWebApi      | Error    | Properties should NOT use required modifier                                                               
 SAASWEB038 | SaaStackWebApi      | Error    | Properties for GET/DELETE requests should all be nullable                                                 
 SAASWEB039 | SaaStackWebApi      | Warning  | Should have summary for documentation                                                                     
 SAASWEB040 | SaaStackWebApi      | Error    | Response must be public                                                                                   
 SAASWEB041 | SaaStackWebApi      | Error    | Response must be named with "Response" suffix                                                             
 SAASWEB042 | SaaStackWebApi      | Error    | Response must be in namespace "Infrastructure.Web.Api.Operations.Shared"                                  
 SAASWEB043 | SaaStackWebApi      | Error    | Response must have a parameterless constructor                                                            
 SAASWEB044 | SaaStackWebApi      | Error    | Properties must have public getters and setters                                                           
 SAASWEB045 | SaaStackWebApi      | Error    | Properties should be nullable not Optional{T} for interoperability                                        
 SAASDDD010 | SaaStackDDD         | Error    | Aggregate roots must have at least one Create() class factory method.                                     
 SAASDDD011 | SaaStackDDD         | Error    | Create() class factory methods must return correct types.                                                 
 SAASDDD012 | SaaStackDDD         | Error    | Aggregate roots must raise a create event in the class factory.                                           
 SAASDDD013 | SaaStackDDD         | Error    | Aggregate roots must only have private constructors.                                                      
 SAASDDD014 | SaaStackDDD         | Error    | Aggregate roots must have a Rehydrate method.                                                             
 SAASDDD015 | SaaStackDDD         | Error    | Dehydratable aggregate roots must override the Dehydrate method.                                          
 SAASDDD016 | SaaStackDDD         | Error    | Dehydratable aggregate roots must declare the EntityNameAttribute.                                        
 SAASDDD017 | SaaStackDDD         | Error    | Properties must not have public setters.                                                                  
 SAASDDD018 | SaaStackDDD         | Error    | Aggregate roots should be marked as sealed.                                                               
 SAASDDD020 | SaaStackDDD         | Error    | Entities must have at least one Create() class factory method.                                            
 SAASDDD021 | SaaStackDDD         | Error    | Create() class factory methods must return correct types.                                                 
 SAASDDD022 | SaaStackDDD         | Error    | Entities must only have private constructors.                                                             
 SAASDDD023 | SaaStackDDD         | Error    | Entities must have a Rehydrate method.                                                                    
 SAASDDD024 | SaaStackDDD         | Error    | Dehydratable entities must override the Dehydrate method.                                                 
 SAASDDD025 | SaaStackDDD         | Error    | Dehydratable entities must declare the EntityNameAttribute.                                               
 SAASDDD026 | SaaStackDDD         | Error    | Properties must not have public setters.                                                                  
 SAASDDD027 | SaaStackDDD         | Error    | Entities should be marked as sealed.                                                                      
 SAASDDD030 | SaaStackDDD         | Error    | ValueObjects must have at least one Create() class factory method.                                        
 SAASDDD031 | SaaStackDDD         | Error    | Create() class factory methods must return correct types.                                                 
 SAASDDD032 | SaaStackDDD         | Error    | ValueObjects must only have private constructors.                                                         
 SAASDDD033 | SaaStackDDD         | Error    | ValueObjects must have a Rehydrate method.                                                                
 SAASDDD034 | SaaStackDDD         | Error    | Properties must not have public setters.                                                                  
 SAASDDD035 | SaaStackDDD         | Error    | ValueObjects must only have immutable methods                                                             
 SAASDDD036 | SaaStackDDD         | Warning  | ValueObjects should be marked as sealed.                                                                  
 SAASDDD036 | SaaStackDDD         | Warning  | ValueObjects should be marked as sealed.                                                                  
 SAASDDD037 | SaaStackDDD         | Warning  | Properties should be Optional{T} not nullable                                                             
 SAASDDD038 | SaaStackDDD         | Error    | Properties are all assigned in ValueObjectBase{T}.GetAtomicValues()                                       
 SAASDDD040 | SaaStackDDD         | Error    | DomainEvents must be public                                                                               
 SAASDDD041 | SaaStackDDD         | Warning  | DomainEvents should be sealed                                                                             
 SAASDDD042 | SaaStackDDD         | Error    | DomainEvents must have a parameterless constructor                                                        
 SAASDDD043 | SaaStackDDD         | Error    | DomainEvents must be named in the past tense                                                              
 SAASDDD045 | SaaStackDDD         | Error    | Create() class factory methods must return correct types                                                  
 SAASDDD046 | SaaStackDDD         | Error    | Properties must have public getters and setters                                                           
 SAASDDD047 | SaaStackDDD         | Error    | Properties must be marked required or nullable or initialized                                             
 SAASDDD048 | SaaStackDDD         | Error    | Properties must be nullable not Optional{T}                                                               
 SAASDDD049 | SaaStackDDD         | Error    | Properties must be of correct type                                                                        
 SAASEVT010 | SaaStackEventing    | Error    | IntegrationEvents must be public                                                                          
 SAASEVT011 | SaaStackEventing    | Warning  | IntegrationEvents should be sealed                                                                        
 SAASEVT012 | SaaStackEventing    | Error    | IntegrationEvents must have a parameterless constructor                                                   
 SAASEVT013 | SaaStackEventing    | Error    | Properties must have public getters and setters                                                           
 SAASEVT014 | SaaStackEventing    | Error    | Properties must be marked required or nullable or initialized                                             
 SAASEVT015 | SaaStackEventing    | Error    | Properties must be nullable not Optional{T}                                                               
 SAASEVT016 | SaaStackEventing    | Error    | Properties must be of correct type                                                                        
 SAASAPP010 | SaaStackApplication | Error    | Resources must be public                                                                                  
 SAASAPP011 | SaaStackApplication | Error    | Resources must have a parameterless constructor                                                           
 SAASAPP012 | SaaStackApplication | Error    | Properties must have public getters and setters                                                           
 SAASAPP013 | SaaStackApplication | Error    | Properties must be nullable not Optional{T}                                                               
 SAASAPP014 | SaaStackApplication | Error    | Properties must of correct type                                                                           
 SAASAPP020 | SaaStackApplication | Error    | ReadModels must be public                                                                                 
 SAASAPP021 | SaaStackApplication | Error    | ReadModels must have the EntityNameAttribute                                                              
 SAASAPP022 | SaaStackApplication | Error    | ReadModels must have a parameterless constructor                                                          
 SAASAPP023 | SaaStackApplication | Error    | Properties must have public getters and setters                                                           
 SAASAPP024 | SaaStackApplication | Warning  | Properties must be Optional{T} not nullable                                                               
 SAASAPP025 | SaaStackApplication | Error    | Properties must of correct type                                                                           
 SAASAPP030 | SaaStackApplication | Error    | Repositories should derive from IApplicationRepository                                                    
 SAASHST010 | SaaStackHosts       | Error    | Aggregate root or Entity should register an identity prefix                                               