using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SND.BusinessManagers.Domain;
using SND.EDM;
using SND.Enums;
using SND.BusinessManagers.Utilities;
using SND.UI.BusinessNavigators;


namespace SND.UI
{
    public partial class StagingPage : BasePage
    {        
        /// <summary>
        /// Property Name: NavigationType.
        /// Method Purpose: This property used to get navigation type current logged on user related GUID.
        /// Author: Mena Armanyous.
        /// Modification Date: April 26, 2011. 
        /// </summary>
        private string NavigationType
        { 
            get
            {
                if (QueryStringManager.ContainsEncryptedKey(QueryStringManager.enumQueryStringKeys.CurrNavigationType) == QueryStringManager.enumQueryStringStatus.Valid_QueryString)
                {
                    return QueryStringManager.GetEncryptedQueryStringValue(QueryStringManager.enumQueryStringKeys.CurrNavigationType);
                }
                return this.Request.QueryString["CurrNavigationType"]; 
            }
        }

        /// <summary>
        /// Property Name: GetNavigationStrategy.
        /// Method Purpose: This method used to create new navigation strategy object based on the passed navigation type.
        /// Author: Mena Armanyous.
        /// Modification Date: May 04, 2011. 
        /// </summary>
        private void GetNavigationStrategyFromURL()
        {
            // clear all states from session before start a new navigation process. 
            NavBase.ClearAllStates();

            // create a new navigation strategy object based on the passed navigation type.
            string lNavigationType = this.NavigationType;
            if (lNavigationType == enumNavigationStrategyType.CardPurchaseStrategy.ToString())
            { _NavBase = new NavCardPurchase(true); }
            else if (lNavigationType == enumNavigationStrategyType.ProductPurchaseStrategy.ToString())
            { _NavBase = new NavCardTopup(true); }
            else if (lNavigationType == enumNavigationStrategyType.CardRegisterationStrategy.ToString())
            { _NavBase = new NavCardRegistration(true); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                //If the logged in user is an internal user then he should be redirected to Page access denied
                //added for patch 8
                if (AccountAdministrationManager.GetCurrentUserID() != Guid.Empty)
                {
                    if (!AccountAdministrationManager.IsCurrentUserInRoles(new string[] { Enum.GetName(typeof(enumRole), enumRole.RegisteredPassenger), Enum.GetName(typeof(enumRole), enumRole.BuyCard) }))
                    {
                        Response.Redirect("~/PageAccessDenied.aspx");
                    }
                }


                // redirect to portal home page in case there is no navigation type passed to the page.
                if (this.NavigationType == null)
                { Response.Redirect("~/Home.aspx"); }

                // get navigation strategy object.
                GetNavigationStrategyFromURL();


                if (this.Request.QueryString["Source"] != null && this.Request.QueryString["Source"] == "Home")
                {
                    _NavBase.NavigationState.HomeNavigation = true;
                }


                // start navigation.
                _NavBase.StartNavigation();
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "StagingPage");
                    ex.Data.Add("EventName", "Page_Load");
                    ExceptionHandler.handle(ex);
                }
                else
                { throw ex; }
            }
        }
    }
}
