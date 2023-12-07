namespace Karami.Core.Domain.Constants;

//Exchanges
public partial class Broker
{
    public const string User_User_Exchange                                      = "User_User_Exchange";
    public const string User_Role_Exchange                                      = "User_Role_Exchange";
    public const string User_Permission_Exchange                                = "User_Permission_Exchange";
    public const string Category_Category_Exchange                              = "Category_Category_Exchange";
    public const string Article_Article_Exchange                                = "Article_Article_Exchange";
    public const string Comment_ArticleComment_Exchange                         = "Comment_ArticleComment_Exchange";
    public const string Comment_ArticleCommentAnswer_Exchange                   = "Comment_ArticleCommentAnswer_Exchange";
    public const string Comment_Article_Exchange_Retry_1                        = "Comment_Article_Exchange_Retry_1";
    public const string Comment_Article_Exchange_Retry_2                        = "Comment_Article_Exchange_Retry_2";
    public const string User_User_Exchange_Retry_1                              = "User_User_Exchange_Retry_1";
    public const string User_User_Exchange_Retry_2                              = "User_User_Exchange_Retry_2";
    public const string User_Role_Exchange_Retry_1                              = "User_Role_Exchange_Retry_1";
    public const string User_Role_Exchange_Retry_2                              = "User_Role_Exchange_Retry_2";
    public const string User_Permission_Exchange_Retry_1                        = "User_Permission_Exchange_Retry_1";
    public const string User_Permission_Exchange_Retry_2                        = "User_Permission_Exchange_Retry_2";
    public const string Auth_User_Exchange_Retry_1                              = "Auth_User_Exchange_Retry_1";
    public const string Auth_User_Exchange_Retry_2                              = "Auth_User_Exchange_Retry_2";
    public const string Auth_Role_Exchange_Retry_1                              = "Auth_Role_Exchange_Retry_1";
    public const string Auth_Role_Exchange_Retry_2                              = "Auth_Role_Exchange_Retry_2";
    public const string Auth_Permission_Exchange_Retry_1                        = "Auth_Permission_Exchange_Retry_1";
    public const string Auth_Permission_Exchange_Retry_2                        = "Auth_Permission_Exchange_Retry_2";
    public const string Article_Category_Exchange_Retry_1                       = "Article_Category_Exchange_Retry_1";
    public const string Article_Category_Exchange_Retry_2                       = "Article_Category_Exchange_Retry_2";
    public const string Article_User_Exchange_Retry_1                           = "Article_User_Exchange_Retry_1";
    public const string Article_User_Exchange_Retry_2                           = "Article_User_Exchange_Retry_2";
    public const string AggregateArticle_Article_Exchange_Retry_1               = "AggregateArticle_Article_Exchange_Retry_1";
    public const string AggregateArticle_Article_Exchange_Retry_2               = "AggregateArticle_Article_Exchange_Retry_2";
    public const string AggregateArticle_ArticleComment_Exchange_Retry_1        = "AggregateArticle_ArticleComment_Exchange_Retry_1";
    public const string AggregateArticle_ArticleComment_Exchange_Retry_2        = "AggregateArticle_ArticleComment_Exchange_Retry_2";
    public const string AggregateArticle_ArticleCommentAnswer_Exchange_Retry_1  = "AggregateArticle_ArticleCommentAnswer_Exchange_Retry_1";
    public const string AggregateArticle_ArticleCommentAnswer_Exchange_Retry_2  = "AggregateArticle_ArticleCommentAnswer_Exchange_Retry_2";
    public const string AggregateArticle_Category_Exchange_Retry_1              = "AggregateArticle_Category_Exchange_Retry_1";
    public const string AggregateArticle_Category_Exchange_Retry_2              = "AggregateArticle_Category_Exchange_Retry_2";
    public const string AggregateArticle_User_Exchange_Retry_1                  = "AggregateArticle_User_Exchange_Retry_1";
    public const string AggregateArticle_User_Exchange_Retry_2                  = "AggregateArticle_User_Exchange_Retry_2";
    public const string StateTracker_Request_Exchange_Retry_1                   = "StateTracker_Request_Exchange_Retry_1";
    public const string StateTracker_Request_Exchange_Retry_2                   = "StateTracker_Request_Exchange_Retry_2";
    public const string StateTracker_Exception_Exchange_Retry_1                 = "StateTracker_Exception_Exchange_Retry_1";
    public const string StateTracker_Exception_Exchange_Retry_2                 = "StateTracker_Exception_Exchange_Retry_2";
    public const string StateTracker_Event_Exchange_Retry_1                     = "StateTracker_Event_Exchange_Retry_1";
    public const string StateTracker_Event_Exchange_Retry_2                     = "StateTracker_Event_Exchange_Retry_2";
    public const string ServiceRegistry_Exchange                                = "ServiceRegistry_Exchange";
    public const string ServiceRegistry_Exchange_Retry_1                        = "ServiceRegistry_Exchange_Retry_1";
    public const string ServiceRegistry_Exchange_Retry_2                        = "ServiceRegistry_Exchange_Retry_2";
    public const string Exception_Exchange                                      = "Exception_Exchange";
    public const string Request_Exchange                                        = "Request_Exchange";
}

//Queues
public partial class Broker
{
    public const string User_User_Queue                                   = "User_User_Queue";                       
    public const string User_User_Queue_Retry                             = "User_User_Queue_Retry";                 
    public const string User_Role_Queue                                   = "User_Role_Queue";                       
    public const string User_Role_Queue_Retry                             = "User_Role_Queue_Retry";                 
    public const string User_Permission_Queue                             = "User_Permission_Queue";                 
    public const string User_Permission_Queue_Retry                       = "User_Permission_Queue_Retry";           
    public const string Auth_User_Queue                                   = "Auth_User_Queue";                       
    public const string Auth_User_Queue_Retry                             = "Auth_User_Queue_Retry";                 
    public const string Auth_Role_Queue                                   = "Auth_Role_Queue";                       
    public const string Auth_Role_Queue_Retry                             = "Auth_Role_Queue_Retry";                 
    public const string Auth_Permission_Queue                             = "Auth_Permission_Queue";                 
    public const string Auth_Permission_Queue_Retry                       = "Auth_Permission_Queue_Retry";           
    public const string Article_Category_Queue                            = "Article_Category_Queue";                
    public const string Article_Category_Queue_Retry                      = "Article_Category_Queue_Retry";
    public const string Article_User_Queue                                = "Article_User_Queue";                
    public const string Article_User_Queue_Retry                          = "Article_User_Queue_Retry";          
    public const string Comment_User_Queue                                = "Comment_User_Queue";          
    public const string Comment_User_Queue_Retry                          = "Comment_User_Queue_Retry";          
    public const string Comment_Article_Queue                             = "Comment_Article_Queue";          
    public const string Comment_Article_Queue_Retry                       = "Comment_Article_Queue_Retry";          
    public const string Comment_ArticleComment_Queue                      = "Comment_ArticleComment_Queue";          
    public const string Comment_ArticleComment_Queue_Retry                = "Comment_ArticleComment_Queue_Retry";
    public const string Comment_ArticleCommentAnswer_Queue                = "Comment_ArticleCommentAnswer_Queue";          
    public const string Comment_ArticleCommentAnswer_Queue_Retry          = "Comment_ArticleCommentAnswer_Queue_Retry";
    public const string AggregateArticle_Article_Queue                    = "AggregateArticle_Article_Queue";        
    public const string AggregateArticle_Article_Queue_Retry              = "AggregateArticle_Article_Queue_Retry";  
    public const string AggregateArticle_ArticleComment_Queue             = "AggregateArticle_ArticleComment_Queue";        
    public const string AggregateArticle_ArticleComment_Queue_Retry       = "AggregateArticle_ArticleComment_Queue_Retry";  
    public const string AggregateArticle_ArticleCommentAnswer_Queue       = "AggregateArticle_ArticleCommentAnswer_Queue";        
    public const string AggregateArticle_ArticleCommentAnswer_Queue_Retry = "AggregateArticle_ArticleCommentAnswer_Queue_Retry";  
    public const string AggregateArticle_User_Queue                       = "AggregateArticle_User_Queue";           
    public const string AggregateArticle_User_Queue_Retry                 = "AggregateArticle_User_Queue_Retry";     
    public const string AggregateArticle_Category_Queue                   = "AggregateArticle_Category_Queue";       
    public const string AggregateArticle_Category_Queue_Retry             = "AggregateArticle_Category_Queue_Retry"; 
    public const string StateTracker_Event_Queue                          = "StateTracker_Event_Queue";
    public const string StateTracker_Event_Queue_Retry                    = "StateTracker_Event_Queue_Retry";
    public const string StateTracker_Exception_Queue                      = "StateTracker_Exception_Queue";
    public const string StateTracker_Exception_Queue_Retry                = "StateTracker_Exception_Queue_Retry";
    public const string StateTracker_Request_Queue                        = "StateTracker_Request_Queue";
    public const string StateTracker_Request_Queue_Retry                  = "StateTracker_Request_Queue_Retry";
    public const string ServiceRegistry_Queue                             = "ServiceRegistry_Queue";
    public const string ServiceRegistry_Queue_Retry                       = "ServiceRegistry_Queue_Retry";
}

//Routes
public partial class Broker
{
    public const string StateTracker_Exception_Route = "StateTracker_Exception_Route";
    public const string StateTracker_Request_Route   = "StateTracker_Request_Route";
    public const string ServiceRegistry_Route        = "ServiceRegistry_Route";
}