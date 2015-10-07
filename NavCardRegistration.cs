using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.Profile;
using SND.Enums;
using SND.EDM;
using SND.BusinessManagers.Domain;
using SND.BusinessManagers.Utilities;
using System.Configuration;
using SND.EDM.PrimitivePOCO;
using System.Web.Security;
using SND.BusinessManagers.Integration;
using SND.BusinessManagers.Integration.BackOffice;

namespace SND.UI.BusinessNavigators
{
    public class NavCardRegistration : NavBase
    {
        #region Private Members

        private BackOfficeManager _BackOfficeManager = null;

        private OnlineAccountsManager _OnlineAccMgr = null;

        #endregion Private Members

        #region Public Members
        /// <summary>
        /// Property Name: OnlineAccMgr.
        /// Property Purpose: This property used to set & get the Online Account Manager.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public OnlineAccountsManager OnlineAccMgr
        {
            get { return this._OnlineAccMgr; }
            set { this._OnlineAccMgr = value; }
        }
        /// <summary>
        /// Property Name: BOManager.
        /// Property Purpose: This property used to set & get the Back Office Manager.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public BackOfficeManager BOManager
        {
            get { return this._BackOfficeManager; }
            set { this._BackOfficeManager = value; }
        }

        /// <summary>
        /// Property Name: NavigationState.
        /// Property Purpose: This property used to set & get the Card Navigation State from Session, you have to call SetNavigationState method at the end of each step.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public override NavigationState NavigationState
        {
            get
            {
                if (SessionManager.Contains(enumSessionKeys.NavCardRegistration))
                { this._NavigationState = SessionManager.Get<NavigationState>(enumSessionKeys.NavCardRegistration); }
                return this._NavigationState as NavigationState;
            }
        }

        /// <summary>
        /// Method Name: CardRegistrationNavigation.
        /// Method Purpose: Pulic default constructor.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public NavCardRegistration(bool pCreateNewNavigationState = false)
        {
            _BackOfficeManager = new BackOfficeManager();
            _OnlineAccMgr = new OnlineAccountsManager();

            if (NavigationState == null)
            {
                if (pCreateNewNavigationState == true)
                {
                    // Try initialize base class.    
                    base.InitializeME();


                    // set new navigation state object.
                    this.SetNavigationState();
                }
                else
                {
                    // handle session time out.
                    base.HandleSessionTimeOut();
                }
            }

            if (_NavigationState.LoggedInUserGUID != Guid.Empty && this._NavigationState.Contact == null)
            {
                this._NavigationState.NavigationPermitted = this.IsNavigationPermitted();

                // set new navigation state object.
                this.SetNavigationState();
            }
        }

        /// <summary>
        /// Method Name: GetList.
        /// Method Purpose: Get specific list type from SND Web Portal database.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011.
        /// </summary>
        /// <param name="pListType"></param>
        /// <returns>Return IList object used to hold the request list type.</returns>
        public override IList GetList(enumListType pListType)
        {
            return null;
        }

        /// <summary>
        /// Method Name: GetAllLists.
        /// Method Purpose: Get specific list type from SND Web Portal database.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2010.
        /// </summary>
        /// <returns>Return 'Hashtable' object used to hold all lists related to current navigation step.</returns>
        public override Hashtable GetAllLists()
        {
            return null;
        }

        /// <summary>
        /// Method Name: SetNavigationState.
        /// Method Purpose: set Navigation State in Session.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        protected override void SetNavigationState()
        {
            try
            {
                if (_NavigationState != null)
                {
                    SessionManager.Set<NavigationState>(enumSessionKeys.NavCardRegistration, this.NavigationState);
                }
                else
                {
                    _NavigationState = new NavigationState();
                    SessionManager.Set<NavigationState>(enumSessionKeys.NavCardRegistration, _NavigationState);
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("CardPurchaseNavigation", "SetNavigationState");
                    ExceptionHandler.handle(ex);
                }
                else
                {
                    throw ex;
                }
            }
        }


        /// <summary>
        /// Method Name: StartNavigation.
        /// Method Purpose:  Start Navigation
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public override void StartNavigation()
        {
            // clear profile properties.
            try
            {

                // validate if the navigation is permitted to current logged on user or not.
                if (this.NavigationState.NavigationPermitted == false)
                { base.HandleNavigationUnPermitted(); }
                else
                {
                    if (CanRegister())
                    {
                        // set navigation type && Navigation step number.
                        this.NavigationState.NavigationStrategyType = enumNavigationStrategyType.CardRegisterationStrategy;
                        this.NavigationState.NavigationStep = enumNavigationStep.StepCardRegistration;

                        // get next navigation page URL.
                        string lNextPageURL = string.Empty;
                        lNextPageURL = this.GetNextNavigationStepURL(this.NavigationState.NavigationStep);

                        // set navigation state.
                        SetNavigationState();

                        HttpContext.Current.Response.Redirect(lNextPageURL);
                    }
                    else
                    {
                        //Redirect to confirmation message

                        _NavigationState.RegistrationProcessCompleted = true;
                        _NavigationState.RegistrationResultMsg = "CardManagement|Error_MaxRegCardCount";
                        _NavigationState.CardRegistrationCompletedFailed = true;

                        HttpContext.Current.Response.Redirect("some url");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "CardRegisteration");
                    ex.Data.Add("EventName", "StartNavigation");
                    ExceptionHandler.handle(ex);
                }
                else
                { throw ex; }
            }
        }

        /// <summary>
        /// Method Name: CancelNavigation.
        /// Method Purpose:  Clears Navigation state and related data
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public override void CancelNavigation()
        {
            try
            {
                // navigate to portal home page.
                string lNextPageURL = string.Empty;
                
                lNextPageURL = "~/Home.aspx";
                HttpContext.Current.Response.Redirect(lNextPageURL);
                
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "CardRegisteration");
                    ex.Data.Add("EventName", "CancelNavigation");
                    ExceptionHandler.handle(ex);
                }
                else
                    throw ex;
            }
        }

        /// <summary>
        /// Method Name: NavigateToNextStep.
        /// Method Purpose:  Navigate to next step.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public override void NavigateToNextStep()
        {
            try
            {
                // validate if the navigation is permitted to current logged on user or not.
                if (this.NavigationState.NavigationPermitted == false)
                { base.HandleNavigationUnPermitted(); }
                else
                {
                    if (this.NavigationState.NavigationStep == enumNavigationStep.StepCardHolderInformation
                        || this.NavigationState.NavigationStep == enumNavigationStep.StepSecurityInformation)
                    {
                        bool lRegisterCardCompleted = GoForRegisretingCard();

                        if (lRegisterCardCompleted == true)
                        { NavigationState.TopupAction = enumTopupAction.TopupRegisteredCard; }
                        NavigationState.CardRegistrationCompletedFailed = !lRegisterCardCompleted;
                        NavigationState.RegisterationCompletedSuccessfully = lRegisterCardCompleted;
                        NavigationState.RegistrationProcessCompleted = true;
                    }

                    // get next navigation step.
                    this.NavigationState.NavigationStep = GetNextNavigationStep();
                   
                    // get next navigation step URL.
                    string lURLRedirect = GetNextNavigationStepURL(this.NavigationState.NavigationStep);

                    // update navigation state.
                    this.SetNavigationState();

                    HttpContext.Current.Response.Redirect(lURLRedirect);
                   
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "CardRegisteration");
                    ex.Data.Add("EventName", "NavigateToNextStep");
                    ExceptionHandler.handle(ex);
                }
                else
                { throw ex; }
            }

        }


        public bool CanRegister()
        {
            //Added by Ahmed Mohsen 25/10/2011- CR#xxxx(Limit Card registeration cards for user to be 5 cards for his online account)
            if (this.NavigationState.Contact != null && this.NavigationState.Contact.Contact_ID > 0)
            {
                CardManager manager = new CardManager();
                int cardCount = manager.GetCards(this.NavigationState.Contact.Contact_ID).Count;
                if (cardCount >= int.Parse(System.Configuration.ConfigurationManager.AppSettings["MaxRegisteredCards"]))
                {
                    return false;
                }
            }
            return true;

            //--------------------------------
        }

        #endregion

        #region Protected Members
        /// <summary>
        /// Method Name: GetNextNavigationStep.
        /// Method Purpose:  Get Next NavigationStep.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        protected override enumNavigationStep GetNextNavigationStep()
        {


            switch (this.NavigationState.NavigationStep)
            {
                case enumNavigationStep.StepCardRegistration://current step
                    if (this.NavigationState.RegistrationProcessCompleted == false)
                    {
                        if (this.NavigationState.LoggedInUserGUID != Guid.Empty)
                        {
                            return enumNavigationStep.StepCardHolderInformation;
                        }
                        else//not logged in
                        {
                            return enumNavigationStep.StepLoginSelection;
                        }
                    }
                    else
                    {
                        return enumNavigationStep.StepHome;
                    }
                case enumNavigationStep.StepUpdateProfile:
                    return enumNavigationStep.StepConfirmation;

                case enumNavigationStep.StepLoginSelection://current step
                    if (this.NavigationState.LoginAction == enumLoginAction.Login)
                    {
                        if (CanRegister() == false)
                        {
                            _NavigationState.CardRegistrationCompletedFailed = true;
                            _NavigationState.RegistrationProcessCompleted = true;
                            _NavigationState.RegistrationResultMsg = "CardManagement|Error_MaxRegCardCount";
                            return enumNavigationStep.StepCardRegistration;
                        }
                        else
                        {
                            return enumNavigationStep.StepCardHolderInformation;
                        }
                    }

                    else if (this.NavigationState.LoginAction == enumLoginAction.Discard)
                    {
                        CancelNavigation();
                    }
                    else if (this.NavigationState.LoginAction == enumLoginAction.Signup)
                    {
                        return enumNavigationStep.StepSignUp;
                    }
                    break;

                case enumNavigationStep.StepCardHolderInformation:
                    return enumNavigationStep.StepConfirmation;

                case enumNavigationStep.StepSignUp://current step
                    return enumNavigationStep.StepSecurityInformation;

                case enumNavigationStep.StepSecurityInformation:
                    if (this.NavigationState.LoggedInUserGUID != Guid.Empty)
                    {
                        return enumNavigationStep.StepConfirmation;
                    }
                    else
                        return enumNavigationStep.StepHome;
            }
            return enumNavigationStep.StepHome;
        }

        /// <summary>
        /// Method Name: GetNextNavigationStepURL.
        /// Method Purpose:  Get Next Navigation Step URL.
        /// Author: Shady Yahia.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        protected override string GetNextNavigationStepURL(enumNavigationStep pNextNavigationStep)
        {
            if (pNextNavigationStep == enumNavigationStep.StepCardRegistration)
            {
                return "SOME URL";
            }
            if (pNextNavigationStep == enumNavigationStep.StepCardHolderInformation)
            {
                return "SOME URL";
            }
            else if (pNextNavigationStep == enumNavigationStep.StepLoginSelection)
            {
                return "SOME URL";
            }
            else if (pNextNavigationStep == enumNavigationStep.StepSignUp)
            {
                return "SOME URL";
            }
            else if (pNextNavigationStep == enumNavigationStep.StepSecurityInformation)
            {
                return "SOME URL";
            }
            else if (pNextNavigationStep == enumNavigationStep.StepConfirmation)
            {
                return "SOME URL";
            }
            else if (pNextNavigationStep == enumNavigationStep.StepHome)
            {

                return "~/Home.aspx";
            }
            else
            {
                return "~/FileNotFound.aspx";
            }
        }

        #endregion

    }
}
