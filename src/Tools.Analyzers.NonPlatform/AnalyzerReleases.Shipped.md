## Release 1.0

### New Rules

 Rule ID | Category            | Severity | Notes                                                                                                     
---------|---------------------|----------|-----------------------------------------------------------------------------------------------------------
 SAS010  | SaaStackWebApi      | Warning  | Methods that are public, should return a Task<T> or just any T, where T is one of the supported results . 
 SAS011  | SaaStackWebApi      | Warning  | These methods must have at least one parameter.                                                           
 SAS012  | SaaStackWebApi      | Warning  | The second parameter can only be a CancellationToken.                                                     
 SAS013  | SaaStackWebApi      | Warning  | These methods must be decorated with a RouteAttribute.                                                    
 SAS014  | SaaStackWebApi      | Warning  | The route (of all these methods in this class) should start with the same path.                           
 SAS015  | SaaStackWebApi      | Warning  | There should be no methods in this class with the same IWebRequest{TResponse}.                            
 SAS016  | SaaStackWebApi      | Warning  | This service operation should return an appropriate Result type for the operation.                        
 SAS017  | SaaStackWebApi      | Warning  | The request type should be declared with a RouteAttribute on it.                                          
 SAS018  | SaaStackWebApi      | Error    | The request type should not be declared with a AuthorizeAttribute on it.                                  
 SAS019  | SaaStackWebApi      | Warning  | The request type should be declared with a AuthorizeAttribute on it.                                      
 SAS030  | SaaStackDDD         | Error    | Aggregate roots must have at least one Create() class factory method.                                     
 SAS031  | SaaStackDDD         | Error    | Create() class factory methods must return correct types.                                                 
 SAS032  | SaaStackDDD         | Error    | Aggregate roots must raise a create event in the class factory.                                           
 SAS033  | SaaStackDDD         | Error    | Aggregate roots must only have private constructors.                                                      
 SAS034  | SaaStackDDD         | Error    | Aggregate roots must have a Rehydrate method.                                                             
 SAS035  | SaaStackDDD         | Error    | Dehydratable aggregate roots must override the Dehydrate method.                                          
 SAS036  | SaaStackDDD         | Error    | Dehydratable aggregate roots must declare the EntityNameAttribute.                                        
 SAS037  | SaaStackDDD         | Error    | Properties must not have public setters.                                                                  
 SAS038  | SaaStackDDD         | Error    | Aggregate roots should be marked as sealed.                                                               
 SAS040  | SaaStackDDD         | Error    | Entities must have at least one Create() class factory method.                                            
 SAS041  | SaaStackDDD         | Error    | Create() class factory methods must return correct types.                                                 
 SAS042  | SaaStackDDD         | Error    | Entities must only have private constructors.                                                             
 SAS043  | SaaStackDDD         | Error    | Entities must have a Rehydrate method.                                                                    
 SAS044  | SaaStackDDD         | Error    | Dehydratable entities must override the Dehydrate method.                                                 
 SAS045  | SaaStackDDD         | Error    | Dehydratable entities must declare the EntityNameAttribute.                                               
 SAS046  | SaaStackDDD         | Error    | Properties must not have public setters.                                                                  
 SAS047  | SaaStackDDD         | Error    | Entities should be marked as sealed.                                                                      
 SAS050  | SaaStackDDD         | Error    | ValueObjects must have at least one Create() class factory method.                                        
 SAS051  | SaaStackDDD         | Error    | Create() class factory methods must return correct types.                                                 
 SAS052  | SaaStackDDD         | Error    | ValueObjects must only have private constructors.                                                         
 SAS053  | SaaStackDDD         | Error    | ValueObjects must have a Rehydrate method.                                                                
 SAS054  | SaaStackDDD         | Error    | Properties must not have public setters.                                                                  
 SAS055  | SaaStackDDD         | Error    | ValueObjects must only have immutable methods                                                             
 SAS056  | SaaStackDDD         | Warning  | ValueObjects should be marked as sealed.                                                                  
 SAS060  | SaaStackDDD         | Error    | DomainEvents must be public                                                                               
 SAS061  | SaaStackDDD         | Warning  | DomainEvents must be sealed                                                                               
 SAS062  | SaaStackDDD         | Error    | DomainEvents must have a parameterless constructor                                                        
 SAS063  | SaaStackDDD         | Error    | DomainEvents must be named in the past tense                                                              
 SAS064  | SaaStackDDD         | Error    | DomainEvents must have at least one Create() class factory method                                         
 SAS065  | SaaStackDDD         | Error    | Create() class factory methods must return correct types                                                  
 SAS066  | SaaStackDDD         | Error    | Properties must have public getters and setters                                                           
 SAS067  | SaaStackDDD         | Error    | Properties must be marked required or nullable or initialized                                             
 SAS068  | SaaStackDDD         | Error    | Properties must be nullable not Optional{T}                                                               
 SAS069  | SaaStackDDD         | Error    | Properties must be of correct type                                                                        
 SAS070  | SaaStackApplication | Error    | Resources must be public                                                                                  
 SAS071  | SaaStackApplication | Error    | Resources must have a parameterless constructor                                                           
 SAS072  | SaaStackApplication | Error    | Properties must have public getters and setters                                                           
 SAS073  | SaaStackApplication | Error    | Properties must be nullable not Optional{T}                                                               
 SAS074  | SaaStackApplication | Error    | Properties must of correct type                                                                           
 SAS080  | SaaStackApplication | Error    | ReadModels must be public                                                                                 
 SAS081  | SaaStackApplication | Error    | ReadModels must have the EntityNameAttribute                                                              
 SAS082  | SaaStackApplication | Error    | ReadModels must have a parameterless constructor                                                          
 SAS083  | SaaStackApplication | Error    | Properties must have public getters and setters                                                           
 SAS084  | SaaStackApplication | Warning  | Properties must be Optional{T} not nullable                                                               
 SAS085  | SaaStackApplication | Error    | Properties must of correct type                                                                           