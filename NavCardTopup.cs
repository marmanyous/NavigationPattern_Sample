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

    public class NavCardTopup : NavBase
    {
        public TopupManager m_TopupManager;

        #region Protected Members
        private ProductManager _ProductManager = new ProductManager();
        #endregion
       
        #region Public Properties
        /// <summary>
        /// Property Name: NavigationState.
        /// Method Purpose: Get navigation state object.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        public override NavigationState NavigationState
        {
            get
            {
                if (SessionManager.Contains(enumSessionKeys.NavCardTopup) == true)
                { base._NavigationState = SessionManager.Get<NavigationState>(enumSessionKeys.NavCardTopup); }
                return base.NavigationState;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Method Name: ProductPurchaseNavigation.
        /// Method Purpose:  Public default contracture.
        /// Author: Mena Armanyous.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public NavCardTopup(bool pCreateNewNavigationState = false)
            : base()
        {
            if (this.NavigationState == null)
            {
                if (pCreateNewNavigationState == true)
                {
                    // Try initialize base class.    
                    base.InitializeME();

                    // create product purchase navigation state object, and add it to the session.    
                    SetNavigationStateLocalMember();

                    // set new navigation state object.
                    this.SetNavigationState();
                }
                else
                {
                    // handle session time out.
                    base.HandleSessionTimeOut();
                }
            }
        }

        /// <summary>
        /// Method Name: GetList.
        /// Method Purpose: Get specific list type from SND Web Portal database.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        /// <param name="pListType"></param>
        /// <returns>Return IList object used to hold the request list type.</returns>
        public override System.Collections.IList GetList(Enums.enumListType pListType)
        {
            try
            {
                switch (pListType)
                {
                    case Enums.enumListType.ConfiguredTopUpList:
                        return SBConfigValues.ListCredit;
                    case Enums.enumListType.RegisteredCardsList:
                        ICollection<Card> lRegisteredCardsList;
                        lRegisteredCardsList = _ProductManager.GetRegisteredCardsList(this.NavigationState.LoggedInUserGUID, "CardRegistered", "CardRegistered.Account");
                        return lRegisteredCardsList.ToList();
                    case Enums.enumListType.LoadLocationList:
                        return SBConfigValues.ListLoadLocation.ToList();
                    case Enums.enumListType.CountriesList:
                        ICollection<SC_CountryCode> lCountriesList;
                        lCountriesList = _ProductManager.GetCountriesList();
                        return lCountriesList.ToList();
                    default:
                        return SearchAllLists(pListType);
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                { ExceptionHandler.handle(ex); }
                else
                { throw ex; }
                return null;
            }
        }

        /// <summary>
        /// Method Name: GetAllLists.
        /// Method Purpose: Get specific list type from SND Web Portal database.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        /// <returns>Return 'Hashtable' object used to hold all lists related to current navigation step.</returns>
        public override System.Collections.Hashtable GetAllLists()
        {
            try
            {
                System.Collections.Hashtable pAllListsCollection = new System.Collections.Hashtable();
                switch (this.NavigationState.NavigationStep)
                {
                    case Enums.enumNavigationStep.StepProductBrowser:
                        IOrderedEnumerable<ConfigSettingValue> lConfiguredTopUpList;
                        lConfiguredTopUpList = _ProductManager.GetConfiguredTopUpList();
                        pAllListsCollection.Add(Enums.enumListType.ConfiguredTopUpList.ToString(), lConfiguredTopUpList.ToList());

                        ICollection<SC_CollectionLocation> lLoadLocationList;
                        lLoadLocationList = _ProductManager.GetLoadLocationList();
                        pAllListsCollection.Add(Enums.enumListType.LoadLocationList.ToString(), lLoadLocationList.ToList());
                        break;
                    case Enums.enumNavigationStep.StepPostalAddress:
                        ICollection<SC_CountryCode> lCountriesList;
                        lCountriesList = _ProductManager.GetCountriesList();
                        pAllListsCollection.Add(Enums.enumListType.CountriesList.ToString(), lCountriesList.ToList());
                        break;
                    case Enums.enumNavigationStep.StepTopupCard:
                        ICollection<Card> lRegisteredCardsList = null;
                        lRegisteredCardsList = _ProductManager.GetRegisteredCardsList(this.NavigationState.LoggedInUserGUID, "CardRegistered", "CardRegistered.Account");
                        pAllListsCollection.Add(Enums.enumListType.RegisteredCardsList.ToString(), lRegisteredCardsList.ToList());
                        break;
                }
                return pAllListsCollection;
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                { ExceptionHandler.handle(ex); }
                else
                { throw ex; }
                return null;
            }
        }

        /// <summary>
        /// Method Name: ClearNavigationState.
        /// Method Purpose: Set navigation state object into session.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        protected override void SetNavigationState()
        {
            if (this.NavigationState != null)
            {

                // update calculated navigation state properties.
                SessionManager.Set<NavigationState>(enumSessionKeys.NavCardTopup, this.NavigationState);
            }
        }

        public Request GetRequestByRequestID(int pRequestID)
        {
            return _ProductManager.GetRequestByID(pRequestID);
        }

        /// <summary>
        /// Method Name: StartNavigation.
        /// Method Purpose: Start navigation.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        public override void StartNavigation()
        {
            try
            {
                NavigationState.NavigationStep = enumNavigationStep.StepHome;

                // navigate to next step
                this.NavigateToNextStep();
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                { ExceptionHandler.handle(ex); }
                else
                { throw ex; }
            }
        }

        /// <summary>
        /// Method Name: CancelNavigation.
        /// Method Purpose: Cancel navigation.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        public override void CancelNavigation()
        {
            try
            {
                // navigate to portal home page.
                string lNextPageURL = string.Empty;
                lNextPageURL = this.GetNextNavigationStepURL(enumNavigationStep.StepHome);
                HttpContext.Current.Response.Redirect(lNextPageURL);
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                { ExceptionHandler.handle(ex); }
                else
                { throw ex; }
            }
        }

        /// <summary>
        /// Method Name: NavigateToNextStep.
        /// Method Purpose: Navigation to next step.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
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

                    // get next navigation step number.
                    this.NavigationState.NavigationStep = GetNextNavigationStep();

                    if (this.NavigationState.NavigationStep == enumNavigationStep.None)
                        SessionManager.Set(enumSessionKeys.TransactionExpired, true);

                    if (this.NavigationState.NavigationStep == enumNavigationStep.StepShoppingBasket)
                    {
                        UpdateSBCollection();
                    }

                    // set new navigation state object.
                    this.SetNavigationState();

                    // redirect to next navigation step related URL.
                    string lNextPageURL = string.Empty;
                    lNextPageURL = this.GetNextNavigationStepURL(this.NavigationState.NavigationStep);
                    HttpContext.Current.Response.Redirect(lNextPageURL);
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                { ExceptionHandler.handle(ex); }
                else
                { throw ex; }
            }
        }

        /// <summary>
        /// Ensure that card exists on BO, issued and not expired
        /// </summary>
        /// <param name="lMsg">the message of error if found</param>
        /// <returns>if card is valid</returns>
        public bool IsCardValid(out string lMsg)
        {
            bool lIsCardValid = true;
            lMsg = "";

            CardManager lCardManager = new CardManager();
            string lCardNumber = _NavigationState.SB_Card.CardNumber;

            BO_CardOverviewResponse lCardOverResponse = lCardManager.GetCardDetails(lCardNumber);
            CardDetails lCardDetails = (lCardOverResponse != null) ? lCardOverResponse.CardOverview : null;

            if (lCardDetails != null)
            {
                enumCardState lCardState = (enumCardState)Enum.Parse(typeof(enumCardState), lCardDetails.CardState);
                //enumCardStatus lCardStatus = (enumCardStatus)Enum.Parse(typeof(enumCardStatus), lCardDetails.CardStatus);

                PurseDetails lPurseDetails = lCardManager.GetCardPurseDetails(lCardNumber);

                //check that the card state, status and purse if exists is valid
                if (lCardState == enumCardState.Issued && lCardDetails.CardStatus == "8"
                    && ((lPurseDetails != null && lPurseDetails.PurseStatus == "8") || lPurseDetails == null))
                {
                    lIsCardValid = true;
                }
                else
                {
                    lMsg = Resources.Resource_2_0.CardSelection_CardNotIssued;
                    lIsCardValid = false;
                }

                //check if the card is expired
                if (lCardDetails.CardExpiryDate < DateTime.Now)
                {

                    lMsg = Resources.Resource_2_0.CardSelection_CardExpired;
                    lIsCardValid = false;
                }
            }
            else
            {
                lMsg = Resources.CardManagement.BONotConnected;
                lIsCardValid = false;
            }

            if (lIsCardValid)
            {
                WriteLogEntry("IsCardValid", _NavigationState.SB_Card.CardNumber + ":Valid");
                NavigationState.SB_Card.CardProfileCode = lCardDetails.CardProfile;

                //Get the code value using the profile code
                if (NavigationState.SB_Card.CardProfileCode != -1)
                {
                    NavigationState.SB_Card.CardProfileValue = new CardManager().GetCardProfileByCodeValue(NavigationState.SB_Card.CardProfileCode.Value).Code_Description;
                    NavigationState.SB_Card.IsPersonalized = CardManager.IsPersonalizedCardProfile((int)NavigationState.SB_Card.CardProfileCode);
                }
            }
            else
                WriteLogEntry("IsCardValid", _NavigationState.SB_Card.CardNumber + ":" + lMsg);

            return lIsCardValid;

        }

        public void LoadCardProfile(BO_CardOverviewResponse pBO_Res)
        {
            if (pBO_Res.CardOverview != null)
            {
                NavigationState.SB_Card.CardProfileCode = pBO_Res.CardOverview.CardProfile;

                //Get the code value using the profile code
                if (NavigationState.SB_Card.CardProfileCode != -1)
                {
                    NavigationState.SB_Card.CardProfileValue = new CardManager().GetCardProfileByCodeValue(NavigationState.SB_Card.CardProfileCode.Value).Code_Description;
                    NavigationState.SB_Card.IsPersonalized = CardManager.IsPersonalizedCardProfile((int)NavigationState.SB_Card.CardProfileCode);
                }
            }
        }

        /// <summary>
        /// Load the BO profile code and value for the card to be topped-up besides loading the SB BO data object 
        /// </summary>
        /// <returns></returns>
        public bool LoadCardInfoIntoSession(BO_CardOverviewResponse pBO_Res)
        {
            try
            {
                WriteLogEntry("LoadCardInfoIntoSession", _NavigationState.SB_Card.CardNumber + ":Start");

                CardManager lCardManager = new CardManager();

                Card lcard = new Card();
                lcard.CardNumber = NavigationState.SB_Card.CardNumber;
                NavigationState.SB_Card.SB_BOdata = lCardManager.GetBackofficeDataForCard(lcard, pBO_Res);

                if (NavigationState.SB_Card.SB_BOdata == null)
                    return false;

                WriteLogEntry("LoadCardInfoIntoSession", _NavigationState.SB_Card.CardNumber + ":Completed");

                return true;
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ExceptionHandler.handle(ex);
                    return false;
                }
                else
                { throw ex; }
            }
        }

        public SC_CollectionLocation GetCollectionLocationObject(int pLocationID)
        {
            return _ProductManager.GetLoadLocationObject(pLocationID);
        }


        #endregion

        #region Protected Methods
        /// <summary>
        /// Method Name: GetNextNavigationStep.
        /// Method Purpose: Get next navigation step number.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        /// <returns>Return next navigation step number.</returns>
        protected override Enums.enumNavigationStep GetNextNavigationStep()
        {
            switch (this.NavigationState.NavigationStep)
            {
                case enumNavigationStep.StepHome:
                    return
                     (this.NavigationState.LoggedInUserGUID == Guid.Empty && this.NavigationState.LoggedInUserName == string.Empty ?
                     this.NavigationState.NavigationStep = enumNavigationStep.StepLoginSelection :
                     this.NavigationState.NavigationStep = enumNavigationStep.StepCardSelection);
                case enumNavigationStep.StepLoginSelection:
                    if (this.NavigationState.LoginAction == enumLoginAction.Signup)
                    {
                        return enumNavigationStep.StepSignUp;
                    }
                    else if (this.NavigationState.LoginAction == enumLoginAction.Login)
                    {
                        return
                        (this.NavigationState.LoggedInUserGUID == Guid.Empty || this.NavigationState.LoggedInUserName == string.Empty ?
                        enumNavigationStep.StepLoginSelection : enumNavigationStep.StepCardSelection);
                    }
                    else if (this.NavigationState.LoginAction == enumLoginAction.Discard)
                    {
                        if (this.NavigationState.RegisterAnonymousCardWhileTopUp == true)
                        { return enumNavigationStep.StepCardRegistration; }
                        else
                        { return enumNavigationStep.StepCardTopup; }
                    }
                    else
                    { return enumNavigationStep.StepLoginSelection; }

                case enumNavigationStep.StepCardSelection:
                    if (this.NavigationState.TopupAction == enumTopupAction.TopupRegisteredCard)
                    {
                        if (this.NavigationState.AutoTopupChecked == true)
                            return enumNavigationStep.StepAutoTopup;
                        else
                            return enumNavigationStep.StepCardTopup;
                    }
                    else if (this.NavigationState.TopupAction == enumTopupAction.TopupAnonymousCard)
                    {
                        if (this.NavigationState.RegisterAnonymousCardWhileTopUp == true)
                        { return enumNavigationStep.StepCardRegistration; }
                        else
                        { return enumNavigationStep.StepCardTopup; }
                    }
                    else
                    { return enumNavigationStep.StepCardTopup; }

                case enumNavigationStep.StepCardRegistration:
                    if (this.NavigationState.ContinueTopupWithoutRegisterCard == false)
                    {
                        if (this.NavigationState.LoggedInUserGUID == Guid.Empty)
                        { return enumNavigationStep.StepSignUp; }
                        else
                        {
                            if (this.NavigationState.LoggedInUserGUID == Guid.Empty)
                            { return enumNavigationStep.StepSignUp; }
                            else
                            { return enumNavigationStep.StepCardHolderInformation; }
                        }
                    }
                    else
                    { return enumNavigationStep.StepCardTopup; }

                case enumNavigationStep.StepCardHolderInformation:
                    return enumNavigationStep.StepConfirmation;

                case enumNavigationStep.StepCardTopup:
                    return
                   enumNavigationStep.StepShoppingBasket;

                case enumNavigationStep.StepSignUp:
                    return enumNavigationStep.StepSecurityInformation;

                case enumNavigationStep.StepSecurityInformation:
                    return enumNavigationStep.StepConfirmation;

                case enumNavigationStep.StepConfirmation:
                    return enumNavigationStep.StepCardTopup;
                default:
                    return enumNavigationStep.None;
            }
        }

        /// <summary>
        /// Method Name: GetNextNavigationStepURL.
        /// Method Purpose: Get next navigation step related URL.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        /// <param name="pCurrNavigationStep"></param>
        /// <returns></returns>
        protected override string GetNextNavigationStepURL(Enums.enumNavigationStep pCurrNavigationStep)
        {
            switch (pCurrNavigationStep)
            {
                case enumNavigationStep.StepLoginSelection:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepCardSelection:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepCardRegistration:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepCardTopup:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepSignUp:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepSecurityInformation:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepCardHolderInformation:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepShoppingBasket:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepConfirmation:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.StepAutoTopup:
                    return QueryStringManager.GetEncryptedPageURL("some url", QueryStringManager.enumQueryStringKeys.NavigationAutoTopup.ToString() + "=true");
                case enumNavigationStep.StepHome:
                    return QueryStringManager.GetPageURL("some url");
                case enumNavigationStep.None:
                    return QueryStringManager.GetPageURL("some url");
                default:
                    return QueryStringManager.GetPageURL("~/FileNotFound.aspx");
            }
        }

        public void ReplaceExistingCardInSB()
        {
            string lStr = NavigationState.SB_Card.CardNumber;
            SB_Card lToBeRemoved = NavigationState.SB_ShoppingBasket.SB_Cards.ToList().Find(c => c.CardNumber == lStr);

            if (lToBeRemoved != null)
            {
                   //_NavigationState._ReplaceCard = true;
                _NavigationState.ReplaceCard = lToBeRemoved;
                _NavigationState.SB_Card.CardProfileCode = lToBeRemoved.CardProfileCode;
                _NavigationState.SB_Card.CardProfileValue = lToBeRemoved.CardProfileValue;
              
            }

        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Method Name: SetNavigationStateLocalMember.
        /// Method Purpose: This method used to set navigation state local member.
        /// Author: Mena Armanyous.
        /// Modification Date: May 2, 2011. 
        /// </summary>
        private void SetNavigationStateLocalMember()
        {
            try
            {
                // create product purchase navigation state object, and add it to the session.

                this.NavigationState.SB_Card = new SB_Card();
                this.NavigationState.SB_Card.IsNew = false;
                this.NavigationState.NavigationStrategyType = enumNavigationStrategyType.CardTopupStrategy;

                SetCommonPurchaseProperties();

            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                { ExceptionHandler.handle(ex); }
                else
                { throw ex; }
            }
        }

        #endregion


    }//end NavTopup

}//end namespace BusinessNavigators
