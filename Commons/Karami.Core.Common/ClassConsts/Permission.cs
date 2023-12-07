using System.Collections.ObjectModel;

namespace Karami.Core.Common.ClassConsts;

public class Permission
{
    #region User
    
    public const string UserReadOne          = "User.ReadOne";
    public const string UserReadAllPaginated = "User.ReadAllPaginated";
    public const string UserCreate           = "User.Create";
    public const string UserUpdate           = "User.Update";
    public const string UserActive           = "User.Active";
    public const string UserInActive         = "User.InActive";
    public const string UserRevoke           = "User.Revoke";

    #endregion

    #region Role

    public const string RoleReadOne          = "Role.ReadOne";
    public const string RoleReadAllPaginated = "Role.ReadAllPaginated";
    public const string RoleCreate           = "Role.Create";
    public const string RoleUpdate           = "Role.Update";
    public const string RoleDelete           = "Role.Delete";

    #endregion
    
    #region Permission

    public const string PermissionReadOne          = "Permission.ReadOne";
    public const string PermissionReadAllPaginated = "Permission.ReadAllPaginated";
    public const string PermissionCreate           = "Permission.Create";
    public const string PermissionUpdate           = "Permission.Update";
    public const string PermissionDelete           = "Permission.Delete";

    #endregion
    
    #region Category

    public const string CategoryReadOne          = "Category.ReadOne";
    public const string CategoryReadAllPaginated = "Category.ReadAllPaginated";
    public const string CategoryCreate           = "Category.Create";
    public const string CategoryUpdate           = "Category.Update";
    public const string CategoryDelete           = "Category.Delete";

    #endregion
    
    #region Article

    public const string ArticleReadOne          = "Article.ReadOne";
    public const string ArticleReadAllPaginated = "Article.ReadAllPaginated";
    public const string ArticleCreate           = "Article.Create";
    public const string ArticleUpdate           = "Article.Update";
    public const string ArticleActive           = "Article.Active";
    public const string ArticleInActive         = "Article.InActive";
    public const string ArticleDelete           = "Article.Delete";

    #endregion
    
    #region ArticleComment

    public const string ArticleCommentReadOne          = "ArticleComment.ReadOne";
    public const string ArticleCommentReadAllPaginated = "ArticleComment.ReadAllPaginated";
    public const string ArticleCommentCreate           = "ArticleComment.Create";
    public const string ArticleCommentUpdate           = "ArticleComment.Update";
    public const string ArticleCommentActive           = "ArticleComment.Active";
    public const string ArticleCommentInActive         = "ArticleComment.InActive";
    public const string ArticleCommentDelete           = "ArticleComment.Delete";

    #endregion
    
    #region ArticleCommentAnswer

    public const string ArticleCommentAnswerReadOne          = "ArticleCommentAnswer.ReadOne";
    public const string ArticleCommentAnswerReadAllPaginated = "ArticleCommentAnswer.ReadAllPaginated";
    public const string ArticleCommentAnswerCreate           = "ArticleCommentAnswer.Create";
    public const string ArticleCommentAnswerUpdate           = "ArticleCommentAnswer.Update";
    public const string ArticleCommentAnswerActive           = "ArticleCommentAnswer.Active";
    public const string ArticleCommentAnswerInActive         = "ArticleCommentAnswer.InActive";
    public const string ArticleCommentAnswerDelete           = "ArticleCommentAnswer.Delete";

    #endregion
    
    #region AggregateArticle

    public const string AggregateArticleReadAllPaginated = "AggregateArticle.ReadAllPaginated";

    #endregion

    private static List<string> Collection = new() {
        UserReadOne                       , //1
        UserReadAllPaginated              , //2
        UserCreate                        , //3
        UserUpdate                        , //4
        UserActive                        , //5
        UserInActive                      , //6
        UserRevoke                        , //7
        RoleReadOne                       , //8
        RoleReadAllPaginated              , //9
        RoleCreate                        , //10
        RoleUpdate                        , //11
        RoleDelete                        , //12
        PermissionReadOne                 , //13
        PermissionReadAllPaginated        , //14
        PermissionCreate                  , //15
        PermissionUpdate                  , //16
        PermissionDelete                  , //17
        CategoryReadOne                   , //18
        CategoryReadAllPaginated          , //19
        CategoryCreate                    , //20
        CategoryUpdate                    , //21
        CategoryDelete                    , //22
        ArticleReadOne                    , //23
        ArticleReadAllPaginated           , //24
        ArticleCreate                     , //25
        ArticleUpdate                     , //26
        ArticleActive                     , //27
        ArticleInActive                   , //28
        ArticleDelete                     , //29
        ArticleCommentReadOne             , //30
        ArticleCommentReadAllPaginated    , //31
        ArticleCommentCreate              , //32
        ArticleCommentUpdate              , //33
        ArticleCommentActive              , //34
        ArticleCommentInActive            , //35
        ArticleCommentDelete              , //36
        AggregateArticleReadAllPaginated  , //37
    };

    public static ReadOnlyCollection<string> GetAll() => Collection.AsReadOnly();
}