## Release 1.0

### New Rules

 Rule ID | Category              | Severity | Notes                                                                                                     
---------|-----------------------|----------|-----------------------------------------------------------------------------------------------------------
 SAS001  | SaaStackDocumentation | Warning  | All public/internal classes, structs, records, interfaces, delegates and enums.                           
 SAS002  | SaaStackDocumentation | Warning  | All public/internal static methods and all public/internal extension methods (in public types).           
 SAS010  | SaaStackWebApi        | Warning  | Methods that are public, should return a Task<T> or just any T, where T is one of the supported results . 
 SAS011  | SaaStackWebApi        | Warning  | These methods must have at least one parameter.                                                           
 SAS012  | SaaStackWebApi        | Warning  | The second parameter can only be a CancellationToken.                                                     
 SAS013  | SaaStackWebApi        | Warning  | These methods must be decorated with a RouteAttribute.                                                    
 SAS014  | SaaStackWebApi        | Warning  | The route (of all these methods in this class) should start with the same path.                           
 SAS015  | SaaStackWebApi        | Warning  | There should be no methods in this class with the same IWebRequest{TResponse}.                            
 SAS016  | SaaStackWebApi        | Warning  | This service operation should return an appropriate Result type for the operation.                        
 SAS017  | SaaStackWebApi        | Warning  | The request type should be declared with a RouteAttribute on it.                                          
 SAS030  | SaaStackDDD           | Error    | Aggregate roots must have at least one Create() class factory method.                                     
 SAS031  | SaaStackDDD           | Error    | Create() class factory methods must return correct types.                                                 
 SAS032  | SaaStackDDD           | Error    | Aggregate roots must raise a create event in the class factory.                                           
 SAS033  | SaaStackDDD           | Error    | Aggregate roots must only have private constructors.                                                      
 SAS034  | SaaStackDDD           | Error    | Aggregate roots must have a Rehydrate method.                                                             
 SAS035  | SaaStackDDD           | Error    | Dehydratable aggregate roots must override the Dehydrate method.                                          
 SAS036  | SaaStackDDD           | Error    | Dehydratable aggregate roots must declare the EntityNameAttribute.                                        
 SAS037  | SaaStackDDD           | Error    | Properties must not have public setters.                                                                  
 SAS040  | SaaStackDDD           | Error    | Entities must have at least one Create() class factory method.                                            
 SAS041  | SaaStackDDD           | Error    | Create() class factory methods must return correct types.                                                 
 SAS042  | SaaStackDDD           | Error    | Entities must only have private constructors.                                                             
 SAS043  | SaaStackDDD           | Error    | Entities must have a Rehydrate method.                                                                    
 SAS044  | SaaStackDDD           | Error    | Dehydratable entities must override the Dehydrate method.                                                 
 SAS045  | SaaStackDDD           | Error    | Dehydratable entities must declare the EntityNameAttribute.                                               
 SAS046  | SaaStackDDD           | Error    | Properties must not have public setters.                                                                  
 SAS050  | SaaStackDDD           | Error    | ValueObjects must have at least one Create() class factory method.                                        
 SAS051  | SaaStackDDD           | Error    | Create() class factory methods must return correct types.                                                 
 SAS052  | SaaStackDDD           | Error    | ValueObjects must only have private constructors.                                                         
 SAS053  | SaaStackDDD           | Error    | ValueObjects must have a Rehydrate method.                                                                
 SAS054  | SaaStackDDD           | Error    | Properties must not have public setters.                                                                  
 SAS055  | SaaStackDDD           | Error    | ValueObjects must only have immutable methods                                                             
