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
using SND.BusinessManagers.Domain.SNDSWCImportService;

namespace SND.UI.BusinessNavigators
{
    /// <summary>
    /// Base class for navigators
    /// </summary>
    public abstract class NavBase
    {
        #region Protected Members
        protected NavigationState _NavigationState;//Protected member used to hold current navigation state object.
        private OnlineAccountsManager _OnlineAccountsManager = new OnlineAccountsManager();
        CardManager _CardManager = new CardManager();
        #endregion

        #region Protected Properties
        // Protected property used to return navigation state object.      
        public virtual NavigationState NavigationState
        {
            get { return _NavigationState; }
        }
        #endregion

        #region Public Abstract Methods
        public abstract System.Collections.IList GetList(enumListType pListType); // Public method used to return a specific list elements.
        public abstract System.Collections.Hashtable GetAllLists(); // Public method used to return all lists related to a specific navigation step number.             

        public abstract void StartNavigation(); // Public method used to start navigation.
        public abstract void CancelNavigation();// Public method used to cancel navigation.
        public abstract void NavigateToNextStep();// Public method used to navigate to next step.
        #endregion

        #region Protected Methods
        protected abstract enumNavigationStep GetNextNavigationStep(); // Protected method used to get next navigation step number.
        protected abstract string GetNextNavigationStepURL(enumNavigationStep pCurrNavigationStep); // Protected method used to return next navigation step related URL.     
        protected abstract void SetNavigationState(); // Public method used to set navigation state into session. 

        protected void InitializeME()
        {
            // Get current logged on user GUID.
            Guid lCurrLoggedOnUserID = AccountAdministrationManager.GetCurrentUserID();

            // Get current  logged on user name from session.
            string lUserName = string.Empty;
            lUserName = AccountAdministrationManager.GetCurrentUserName();

            // create navigation state object.
            if (lCurrLoggedOnUserID == Guid.Empty || lUserName == string.Empty)
            { _NavigationState = new NavigationState(); }
            else
            {
                _NavigationState = new NavigationState(lCurrLoggedOnUserID, lUserName);

                // Try update navigation state contact data.
                this.UpdateNavigationStateContactData();
            }
        }

        protected System.Collections.IList SearchAllLists(Enums.enumListType pListType)
        {
            switch (pListType)
            {
                case Enums.enumListType.ConfiguredTopUpList:
                    return SBConfigValues.ListCredit;
                case Enums.enumListType.RegisteredCardsList:
                    ICollection<Card> lRegisteredCardsList;
                    lRegisteredCardsList = new ProductManager().GetRegisteredCardsList(this.NavigationState.LoggedInUserGUID, "CardRegistered", "CardRegistered.Account");
                    return lRegisteredCardsList.ToList();
                case Enums.enumListType.LoadLocationList:
                    return SBConfigValues.ListLoadLocation.ToList();
                case Enums.enumListType.CountriesList:
                    ICollection<SC_CountryCode> lCountriesList;
                    lCountriesList = new ProductManager().GetCountriesList();
                    return lCountriesList.ToList();
                case enumListType.CardProfileList:
                    ICollection<SC_CardProfile> lCardProfileList;
                    lCardProfileList = new SchemeCodesManager().GetSchemeCode<SC_CardProfile>();
                    return lCardProfileList.ToList();
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Method Name: HandleSessionTimeOut.
        /// Method Purpose:  Protected method used to handle session time out.
        /// Author: Mena Armanyous.
        /// Modification Date: May 10, 2011. 
        /// </summary>
        protected void HandleSessionTimeOut()
        {
            try
            {
                string lNextPageURL = string.Empty;
                lNextPageURL = QueryStringManager.GetPageURL("~/FileNotFound.aspx");
                HttpContext.Current.Response.Redirect(lNextPageURL);
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("ClassName", "NavBase");
                    ex.Data.Add("MethodName", "HandleSessionTimeOut");
                    ExceptionHandler.LogException(ex);
                }
                else
                { throw ex; }
            }           
        }



        protected void SaveShoppingBasketIntoState()
        {

            NavigationState.SB_ShoppingBasket = SessionManager.Get<SB_ShoppingBasket>(enumSessionKeys.ShoppingBasket);
            if (NavigationState.SB_ShoppingBasket == null ||
                (NavigationState.SB_ShoppingBasket != null && NavigationState.SB_ShoppingBasket.SB_Cards.Count == 0))
            {
                NavigationState.SB_ShoppingBasket = new SB_ShoppingBasket();
                new SBManager().SetImortInfoInSB(NavigationState.SB_ShoppingBasket);
            }

          
        }

        /// <summary>
        /// Update the SB collection with the new card
        /// </summary>
        protected void UpdateSBCollection()
        {
            try
            {
                WriteLogEntry("UpdateSBCollection", _NavigationState.SB_Card.CardNumber + ":Start");

                // to prevent changing page from the address bar without filling the card info
                if (_NavigationState.SB_Card.CreditValue == 0 && _NavigationState.SB_Card.SB_Tickets.Count == 0)
                {
                    StartNavigation();
                }

                if (_NavigationState.LoggedInUserGUID != Guid.Empty)
                    _NavigationState.SB_ShoppingBasket.Contact_ID = _NavigationState.Contact.Contact_ID;

                SBManager mng = new SBManager();

                //to set the total and sub-total values
                mng.CalculateCardTotals(_NavigationState.SB_Card);

                int oldSBID = _NavigationState.SB_ShoppingBasket.SB_ShoppingBasket_Id;
                mng.UpdateShoppingBasket(_NavigationState.SB_ShoppingBasket);
                _NavigationState.SB_ShoppingBasket.SessionID = HttpContext.Current.Session.SessionID;
                _NavigationState.SB_ShoppingBasket.SB_Cards.Add(_NavigationState.SB_Card);
                //_NavigationState.SB_Card.SB_ShoppingBasket = _NavigationState.SB_ShoppingBasket;

                //handle if the card already exists on SB
                if (_NavigationState.ReplaceCard != null)
                {
                    mng.RemoveSBCard(_NavigationState.ReplaceCard);
                    _NavigationState.SB_ShoppingBasket.SB_Cards.Remove(_NavigationState.ReplaceCard);
                }

                mng.SaveSB();

                //if user is anonymous then set the shopping basket id to a cookie
                if (!HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    mng.SetSB4AnonymousUser(_NavigationState.SB_ShoppingBasket);
                }

                SessionManager.Set<SB_ShoppingBasket>(enumSessionKeys.ShoppingBasket, _NavigationState.SB_ShoppingBasket);

                NavigationState.ScenarioCompleted = true;

                WriteLogEntry("UpdateSBCollection", _NavigationState.SB_Card.CardNumber + ":Completed");

            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("ClassName", "NavBase");
                    ex.Data.Add("MethodName", "UpdateSBCollection");
                    ExceptionHandler.LogException(ex);
                }
                else
                { throw ex; }
            }
        }

        /// <summary>
        /// set the common card proeprties on both topup and card purchase scenairos in the session state
        /// </summary>
        protected void SetCommonPurchaseProperties()
        {
            SaveShoppingBasketIntoState();

            //if the cards number in SB equals to or larger than the maximum allowed cards on SB
            int newcards = _NavigationState.SB_ShoppingBasket.SB_Cards.Where(x => x.IsNew).Sum(x => x.Quantity);
            int existcards = _NavigationState.SB_ShoppingBasket.SB_Cards.Count(x => !x.IsNew);
            int totcards = newcards + existcards;

            if (totcards >= SBConfigValues.SBConfigList.MaxCardsPerSB)
            {
                string lURL = QueryStringManager.GetEncryptedPageURL("SB URL",
                    QueryStringManager.enumQueryStringKeys.MaxCardsExceeded.ToString() + "=true");

                HttpContext.Current.Response.Redirect(lURL);
            }
            else if (new SND.BusinessManagers.Domain.ConfigurationManager().IsChildPersonalizedVerificationProcessEnabled() &&
                _NavigationState.SB_ShoppingBasket.SB_Cards.Any(card => card.CardProfileCode == SWC.SC_CardProfile.PersonalizedChild))
            {
                string lURL = QueryStringManager.GetEncryptedPageURL("SB URL",
                    QueryStringManager.enumQueryStringKeys.SBContainsPersonalizedCard.ToString() + "=true");

                HttpContext.Current.Response.Redirect(lURL);
            }

            this.NavigationState.SB_Card.GuidID = Guid.NewGuid();
            this.NavigationState.NavigationPermitted = this.IsNavigationPermitted();

            SessionManager.Set<enumPurchaseTransactionStep>(enumSessionKeys.ShoppingBasketTransactionStatus, enumPurchaseTransactionStep.None);
        }

        /// <summary>
        /// Method Name: HandleNavigationUnPermitted.
        /// Method Purpose:  Protected method used to handle navigation UnPermitted.
        /// Author: Mena Armanyous.
        /// Modification Date: May 12, 2011. 
        /// </summary>
        protected void HandleNavigationUnPermitted()
        {
            try
            {
                string lNextPageURL = string.Empty;
                lNextPageURL = QueryStringManager.GetPageURL("~/PageAccessDenied.aspx");
                HttpContext.Current.Response.Redirect(lNextPageURL);
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("ClassName", "NavBase");
                    ex.Data.Add("MethodName", "HandleNavigationUnPermitted");
                    ExceptionHandler.LogException(ex);
                }
                else
                { throw ex; }
            }           
        }

        /// <summary>
        /// Method Name: UpdateNavigationState 
        /// Method: This method used to update navigation state.
        /// Added by:Mena Armanyous
        /// Modified on: 12-April-2011
        /// </summary>
        /// <param name="pMessage"></param>
        protected void UpdateNavigationState(Guid pSignedUpID)
        {
            if (pSignedUpID != null && pSignedUpID != Guid.Empty)
            {
                // update logged on user ID & user name.
                _NavigationState.LoggedInUserGUID = pSignedUpID;
                if (_NavigationState.Contact != null)
                { _NavigationState.LoggedInUserName = _NavigationState.Contact.Account.aspnet_Users.UserName; }

                // update navigation state.
                this.SetNavigationState();
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Method Name: UpdateNavigationStateContactData.
        /// Method Purpose: This method .
        /// Author: Mena Armanyous.
        /// Modification Date: May 04, 2011.
        /// </summary>
        public Contact UpdateNavigationStateContactData(Contact pContact = null)
        {
            Contact lTempContact;
            lTempContact = _OnlineAccountsManager.GetContactByUserId(_NavigationState.LoggedInUserGUID, "Contact", "Contact.Addresses", "Contact.Addresses.SC_CountryCode", "Contact.Addresses.SC_CountryCode.SchemeCodesImport");
            _NavigationState.Contact = lTempContact;

            // Refresh navigation permitted flag after login.
            _NavigationState.NavigationPermitted = this.IsNavigationPermitted();

            return lTempContact;
        }


        /// <summary>
        /// Method Name: IsNavigationPermitted.
        /// Method Purpose: Check if navigation permitted.
        /// Author: Mena Armanyous.
        /// Modification Date: May 04, 2011.
        /// </summary>
        /// <returns></returns>
        public bool IsNavigationPermitted()
        {
            if (this._NavigationState.LoggedInUserGUID == Guid.Empty)
            { return true; }
            else
            {
                string[] lPermittedRoles = new string[] { Enum.GetName(typeof(enumRole), enumRole.RegisteredPassenger), Enum.GetName(typeof(enumRole), enumRole.BuyCard) };
                return AccountAdministrationManager.IsUserInRoles(lPermittedRoles, this._NavigationState.LoggedInUserGUID);
            }   
        }

        /// <summary>
        /// Perform the registration process
        /// </summary>
        /// <returns>If the operation completed successfully</returns>
        protected bool GoForRegisretingCard()
        {
            bool lRegisterCardCompleted = false;
            string lBackendID = _CardManager.GetBackendIDForCard(_NavigationState.RegisteredCard);

            #region Get Card profile

            //add the card profile to the cardregistered object
            _NavigationState.RegisteredCard.CardRegistered.CardProfileCode = _CardManager.GetCardProfileCode(_NavigationState.RegisteredCard.CardNumber);

            if (_NavigationState.RegisteredCard.CardRegistered.CardProfileCode == -1)
            {
                lRegisterCardCompleted = false;

                NavigationState.RegistrationResultMsg = "CardManagement|BONotConnected";

                WriteLogEntry("GoForRegisretingCard", "Retrieving card profile for Card:" + _NavigationState.RegisteredCard.CardNumber + " Failed");

                return lRegisterCardCompleted;
            }

            #endregion

            #region Get card expiry date

            //add the expiry date to the card registered
            BO_CardOverviewResponse resOverview = _CardManager.GetCardDetails(_NavigationState.RegisteredCard.CardNumber);
            if (resOverview == null || resOverview.CardOverview == null)
            {
                lRegisterCardCompleted = false;
                NavigationState.RegistrationResultMsg = "CardManagement|BONotConnected";

                WriteLogEntry("GoForRegisretingCard", "Retrieving card expiry date for Card:" + _NavigationState.RegisteredCard.CardNumber + " Failed");

                return lRegisterCardCompleted;
            }
            _NavigationState.RegisteredCard.CardRegistered.CardExpiryDate = resOverview.CardOverview.CardExpiryDate;

            #endregion

            WriteLogEntry("GoForRegisretingCard", _NavigationState.RegisteredCard.CardNumber + ":Backend ID:" + lBackendID);

            //if the backend id is existing on BO, just save it on DB without calling the registration BO
            if (lBackendID != string.Empty)
            {
                _NavigationState.RegisteredCard.CardRegistered.BackEnd_ID = lBackendID;

                if (NavigationState.TempCardHolderContact != null)
                {
                    CloneContact(NavigationState.TempCardHolderContact, NavigationState.Contact);
                }

                if (_CardManager.IsCardRegistered(this._NavigationState.RegisteredCard.CardNumber))
                {
                    NavigationState.RegistrationResultMsg = "CardManagement|CardRegisteredErrorMsg_Registered";
                    lRegisterCardCompleted = false;
                }
                else
                {
                    lRegisterCardCompleted = SaveCardRegisteration();
                    if (lRegisterCardCompleted)
                    {
                        NavigationState.RegistrationResultMsg = "CardManagement|SuccessMsg";

                        //Auditing
                        AuditManagerBase.CommonAuditParams commonAuditParam = new AuditManagerBase.CommonAuditParams(_NavigationState.Contact.Contact_ID, DateTime.Now, (int?)_NavigationState.Contact.Contact_ID, _NavigationState.RegisteredCard.Card_ID, "B/O Registration", "Contact:" + _NavigationState.Contact.Contact_ID);
                        AuditManager.AuditBORegisteration(commonAuditParam, "", "Personalized card registration on web portal");

                        //send confirmation mail
                        SendCardRegistrationConfirmationMail();


                    }
                    else
                    {
                        NavigationState.RegistrationResultMsg = "CardManagement|SavingErrorMsg";
                    }
                }


            }
            //Backend id is empty, so call the registration API
            else
            {
                string lMsg = string.Empty;
                lRegisterCardCompleted = RegisterCard(out lMsg);
                NavigationState.RegistrationResultMsg = lMsg;

            }

            WriteLogEntry("GoForRegisretingCard", _NavigationState.RegisteredCard.CardNumber + ":" + _NavigationState.RegistrationResultMsg);

            return lRegisterCardCompleted;

        }


        /// <summary>
        /// Method Name: RegisterCard
        /// Method Description: call back office manager to register card
        /// </summary>
        /// <param name="pContact">Contact</param>
        /// <param name="pAddress">Contact Address</param>
        /// <param name="pCard">Card for registration</param>
        /// <returns>BO_CardRegisterationResponse</returns>
        /// <Author>Mena Armanyous</Author>     
        /// <Modified on>7-May-2011</Modified>
        public bool RegisterCard(out string pMessage)
        {
            BackOfficeManager _BackOfficeManager = new BackOfficeManager();
            BO_CardRegisterationResponse lBOResponse = null;

            try
            {
                
                //Register
                Address lAddress = this.NavigationState.Contact.Addresses.FirstOrDefault();
                if (System.Configuration.ConfigurationManager.AppSettings["IsBackOfficeConnected"].ToLower().Equals("false"))
                {
                    lBOResponse = new BO_CardRegisterationResponse();
                    lBOResponse.ResponseCode = _BackOfficeManager.RES_SUCCESS;
                    lBOResponse.ResponseCodePhrase = "BORegisteredSuccessfully";
                    lBOResponse.BackendID = "111114";
                    //this.NavigationState.BackOfficeResponse = lBOResponse;
                }
                else
                {
                    lBOResponse = _BackOfficeManager.RegisterCard(this.NavigationState.Contact, this.NavigationState.RegisteredCard, lAddress);
                }

                StringBuilder strMessage = new StringBuilder();
                // Log Event
                string logTitle = "Card Registration ";
                strMessage.AppendLine("Response Code:" + lBOResponse.ResponseCode);
                strMessage.AppendLine("Response Phrase:" + lBOResponse.ResponseCodePhrase);
                strMessage.AppendLine("Backend ID:" + lBOResponse.BackendID);

                Logger.LogMessage(logTitle, strMessage.ToString(), LogEntryCategory.Development, System.Diagnostics.TraceEventType.Information, LogEntryPriority.Highest);


                //process response

                bool result = ProcessCardRegisterationResponse(lBOResponse, out pMessage);

                return result;

                //else
                //{
                //    SessionManager.Set(enumSessionKeys.IsError, true);
                //    SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                //    pMessage = HttpContext.GetGlobalResourceObject("CardManagement", "BONotConnected").ToString();
                //    return false;
                //}
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "CardRegistration");
                    ex.Data.Add("EventName", "RegisterCard");
                    if (lBOResponse != null)
                    {
                        ex.Data.Add("Response Code:", lBOResponse.ResponseCode);
                        ex.Data.Add("Response Desc:", lBOResponse.ResponseCodePhrase);
                    }
                    ExceptionHandler.LogException(ex);
                }
                else
                { throw ex; }
                SessionManager.Set(enumSessionKeys.IsError, true);
                SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                pMessage = HttpContext.GetGlobalResourceObject("CardManagement", "InconvenienceErrorMsg").ToString();
                return false;
            }
        }

        /// <summary>
        /// Method Name: ProcessCardRegisterationResponse
        /// Method Description: process the back office response
        /// </summary>
        /// <param name="pResponse">Back office response</param>
        /// <param name="pContact">Contact</param>
        /// <param name="pCard">Card</param>
        /// <Author> Mena Armanyous</Author>
        /// <Modified on>1-Jan-2011</Modified>
        protected bool ProcessCardRegisterationResponse(BO_CardRegisterationResponse pResponse,out string pMessage)
        {
            try
            {
                Card pCard = NavigationState.RegisteredCard;
                Contact pContact = NavigationState.Contact;
                BackOfficeManager lBOMgr = new BackOfficeManager();

                if (pResponse == null)
                {
                    // other error 
                    // Display confirmation message page with explanation message
                    SessionManager.Set(enumSessionKeys.IsError, true);
                    SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                    pMessage = Resources.CardManagement.BONotConnected;  //"CardManagement|BONotConnected";
                    return false;
                    // DisplayConfirmationMessage(lMsg, true);
                }
                else
                {
                    if (pResponse.ResponseCode == lBOMgr.RES_APPLICATION_INSERTION_ERROR ||
                        pResponse.ResponseCode == lBOMgr.RES_APPLICATION_SELECTION_ERROR ||
                        pResponse.ResponseCode == lBOMgr.RES_ERROR)
                    {
                        // Application error
                        // Display confirmation message page with inconvenience error message
                        SessionManager.Set(enumSessionKeys.IsError, true);
                        SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                        pMessage = Resources.CardManagement.BONotConnected; // "CardManagement|BONotConnected";
                        return false;
                        // DisplayConfirmationMessage(lMsg, true);
                    }
                    else if (pResponse.ResponseCode == lBOMgr.RES_REGISTERD_CARD)
                    {
                        // card already registered
                        // Display confirmation message page with please register valid card number
                        SessionManager.Set(enumSessionKeys.IsError, true);
                        SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                        pMessage = Resources.CardManagement.CardRegisteredErrorMsg;// "CardManagement|CardRegisteredErrorMsg";
                        return false;
                        // DisplayConfirmationMessage(lMsg, true);
                    }
                    else if (pResponse.ResponseCode == lBOMgr.RES_INPUTS_ARE_BLANK ||
                             pResponse.ResponseCode == lBOMgr.RES_INPUT_FORMAT_ERROR)
                    {
                        // card already registered
                        // Display confirmation message page with please register valid card number
                        SessionManager.Set(enumSessionKeys.IsError, true);
                        SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                        pMessage = Resources.CardManagement.InputsAreBlankErrorMsg; // "CardManagement|InputsAreBlankErrorMsg";
                        return false;
                        // DisplayConfirmationMessage(lMsg, true);
                    }
                    else if (pResponse.ResponseCode == lBOMgr.RES_SUCCESS)
                    {
                        // success
                        // Save record to database
                        pCard.CardRegistered.BackEnd_ID = pResponse.BackendID;

                        if (NavigationState.TempCardHolderContact != null)
                        {
                            CloneContact(NavigationState.TempCardHolderContact, NavigationState.Contact);
                        }
                        if (_CardManager.IsCardRegistered(pCard.CardNumber))
                        {
                            NavigationState.RegistrationResultMsg = "CardManagement|CardRegisteredErrorMsg_Registered";
                            SessionManager.Set(enumSessionKeys.IsError, true);
                            SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                            pMessage = Resources.CardManagement.CardRegisteredErrorMsg_Registered; // "CardManagement|SavingErrorMsg";
                            return false;
                        }
                    
                        if (SaveCardRegisteration())//SaveCardRegisteration(pCard))
                        {
                            //Auditing
                            AuditManagerBase.CommonAuditParams commonAuditParam = new AuditManagerBase.CommonAuditParams(_NavigationState.Contact.Contact_ID, DateTime.Now, (int?)_NavigationState.Contact.Contact_ID, _NavigationState.RegisteredCard.Card_ID, "B/O Registration", "Contact:" + _NavigationState.Contact.Contact_ID);
                            AuditManager.AuditBORegisteration(commonAuditParam, pResponse.ResponseCode, pResponse.ResponseCodePhrase);
                            ///////////////////////////////////////////

                            // Send Notification mail
                            SendCardRegistrationConfirmationMail();// (pCard, pContact);

                            // Saving card succeeded, display success message
                            SessionManager.Set(enumSessionKeys.IsError, false);
                            SessionManager.Set(enumSessionKeys.ConfirmationTitle, Resources.PagesTitles.Confirmation_Title);
                            pMessage = Resources.CardManagement.SuccessMsg; //"CardManagement|SuccessMsg";
                            return true;
                            // DisplayConfirmationMessage(lMsg);
                        }
                        else
                        {
                            // saving card failed, display error message
                            SessionManager.Set(enumSessionKeys.IsError, true);
                            SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                            pMessage = Resources.CardManagement.SavingErrorMsg; // "CardManagement|SavingErrorMsg";
                            return false;
                            //  DisplayConfirmationMessage(lMsg, true);
                        }
                    }
                    else if (pResponse.ResponseCode == lBOMgr.RES_BLOCKED_CARD_PURSE ||
                             pResponse.ResponseCode == lBOMgr.RES_EXPIRED_CARD_PURSE ||
                             pResponse.ResponseCode == lBOMgr.RES_CLOSED_OFF_CARD_PURSE ||
                             pResponse.ResponseCode == lBOMgr.RES_BLACKLISTED_CARD_PURSE)
                    {
                        // card related error
                        SessionManager.Set(enumSessionKeys.IsError, true);
                        SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                        pMessage = Resources.CardManagement.CardErrorMsg + pResponse.ResponseCodePhrase;
                        return false;
                        //DisplayConfirmationMessage(lMsg, true);

                    }
                    else if (pResponse.ResponseCode == lBOMgr.RES_PATRONS_RECORD_NOT_FOUND || pResponse.ResponseCode == lBOMgr.RES_NO_CARD_MASTER)
                    {
                        // card not exists in back office error
                        // Display confirmation message page with explanation message
                        SessionManager.Set(enumSessionKeys.IsError, true);
                        SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                        pMessage = Resources.CardManagement.RegisterCard_Patrons_Record_Not_Found; // "CardManagement|RegisterCard_Patrons_Record_Not_Found";
                        return false;
                        //DisplayConfirmationMessage(lMsg, true);
                    }
                    else
                    {
                        // other error 
                        // Display confirmation message page with explanation message
                        SessionManager.Set(enumSessionKeys.IsError, true);
                        SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                        pMessage = Resources.CardManagement.BONotConnected; // "CardManagement|BONotConnected";
                        return false;
                        //DisplayConfirmationMessage(lMsg, true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "ContactInformation");
                    ex.Data.Add("EventName", "ProcessCardRegisterationResponse");
                    ExceptionHandler.handle(ex);
                }
                else
                { throw ex; }
                SessionManager.Set(enumSessionKeys.IsError, true);
                SessionManager.Set(enumSessionKeys.ConfirmationTitle, "Error");
                pMessage = HttpContext.GetGlobalResourceObject("CardManagement", "InconvenienceErrorMsg").ToString();
                return false;
            }
        }

        

        /// <summary>
        /// Method Name: SignUp.
        /// Method Purpose: This method used to sign up.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        /// <param name="pASPNetUserID"></param>
        /// <param name="pAccount"></param>
        /// <param name="pContact"></param>
        /// <param name="pNewsLetterSubscribe"></param>
        /// <returns></returns>
        public bool SignUp(Guid pASPNetUserID, Account pAccount, Contact pContact, bool pNewsLetterSubscribe)
        {
            try
            {
                //set the newly created user as logged in
                pAccount.IsLoggedIn = true;

                // create new contact with its related account information.
                pContact.Account = pAccount;
               
                OnlineAccountsManager lOnlineManager = new OnlineAccountsManager();
                lOnlineManager.CreateContact(pContact);
                
                //set aspnet_users for account
                pContact.Account.aspnet_Users = new AccountAdministrationManager().GetAspNetUser(pASPNetUserID);
                
                this.SendSignUpConfirmationMail(pContact, HttpContext.Current.Request.IsSecureConnection);

                #region Athenticate user
                // authenticate signed up user.
                string strUserName = (pContact.Account.aspnet_Users.UserName);
                SessionManager.Set<string>(enumSessionKeys.UserName,strUserName);
                FormsAuthentication.SetAuthCookie(strUserName, false);
                
                #endregion

                // update signed up user related navigation state.
                UpdateNavigationState(pASPNetUserID);

                //updated by Randa Salah Eldin as newsletter will not be connected with the Account anymore
                if (pNewsLetterSubscribe)
                {
                    NewsletterManager lNewsletterManager = new NewsletterManager();

                    if (pNewsLetterSubscribe && lNewsletterManager.IsUserSubscribed(pContact.Email) == false)
                    {

                        lNewsletterManager.AddNewsletterSubscriber(pContact.Email);
                    }
                    else if (pNewsLetterSubscribe == false && lNewsletterManager.IsUserSubscribed(pContact.Email) == true)
                    {
                        lNewsletterManager.UnsubscribeFromNewsLetter(pContact.Email);
                    }
                }

                #region Session Fixation Security Threat - FIX
                // written by rabie @ 15 july 2012
                new AccountAdministrationManager().RegenerateSessionID(false,true);
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("ClassName", "NavBase");
                    ex.Data.Add("MethodName", "SignUp");
                    ExceptionHandler.handle(ex);                   
                }
                else
                { throw ex; }
                return false;
            }
        }

        /// <summary>
        /// Method Name: WriteLogEntry.
        /// Method Purpose: This method used to write new log enSND log file.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011.
        /// </summary>
        /// <param name="pLogEntryTitle"></param>
        /// <param name="pLogEntryText"></param>
        public void WriteLogEntry(string pLogEntryTitle, string pLogEntryText)
        {Logger.LogMessage(pLogEntryTitle, pLogEntryText, LogEntryCategory.Development, System.Diagnostics.TraceEventType.Information);}


        /// <summary>
        /// Method Name: IsCardValid.
        /// Method Purpose:  To check at BackOffice if Card is in valid format.
        /// Modification Date: April 28, 2011. 
        /// </summary>
        public bool IsCardValid(string pCardNumber,out string pMsg)
        {
            try
            {
                pMsg = "";
                BackOfficeManager _mngr = new BackOfficeManager();
                
                bool lCardValid = _mngr.IsCardNumberValid(pCardNumber);
                if(lCardValid == false)
                    pMsg = Resources.SelfServices.TopupAnonymousCard_CardNoValidate;

                if (lCardValid == true)
                {
                    if (System.Configuration.ConfigurationManager.AppSettings["IsBackOfficeConnected"].ToLower().Equals("true"))
                    {
                        BO_Response lRes = _mngr.CheckCardSerialNumber(pCardNumber);

                        if (lRes != null && (lRes.ResponseCode == _mngr.RES_SUCCESS || lRes.ResponseCode == _mngr.CSN_IS_NOT_ANONYMOUS))
                        {
                            lCardValid = true;
                        }
                        else
                        {
                            lCardValid = false;
                            if (lRes.ResponseCode == _mngr.RES_ERROR)
                            {
                                pMsg = Resources.CardManagement.BONotConnected;
                            }
                            else
                            {
                                pMsg = Resources.SelfServices.TopupAnonymousCard_CardNoValidate;
                            }
                        }
                    }
                    else
                    {
                        lCardValid = true;
                    }
                }

                return lCardValid;
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "CardRegisteration");
                    ex.Data.Add("EventName", "IsCardValid");
                    ExceptionHandler.LogException(ex);
                }
                else throw ex;
                pMsg = "";
                return false;
            }


        }


        public bool IsCardRegistered(string pCardNumber, out string pMsg)
        {
            try
            {
                pMsg = "";

                bool lCardRegistered = _CardManager.IsCardRegistered(pCardNumber);

                if (lCardRegistered)
                {
                    pMsg = Resources.SelfServices.TopupAnonymousCard_CardRegistered;
                }


                return lCardRegistered;
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "CardRegisteration");
                    ex.Data.Add("EventName", "IsCardRegistered");
                    ExceptionHandler.LogException(ex);
                }
                else throw ex;
                pMsg = "";
                return false;
            }


        }

        /// <summary>
        /// Method Name: SendSignUpConfirmationMail.
        /// Method Purpose: This method used to send signup confirmation mail.
        /// Author: Mena Armanyous.
        /// Modification Date: May 08, 2011.
        /// </summary>
        /// <param name="pContact"></param>
        /// <param name="pIsSecureConnection"></param>
        public bool SendSignUpConfirmationMail(Contact pContact, bool pIsSecureConnection)
        {
            try
            {
                //forming the notification mail then sent
                PrimitiveContact lSerContact = new PrimitiveContact(pContact);

                string strhttp = "http";
                if (pIsSecureConnection == true)
                { strhttp += "s"; }

                string lContactUsPath = "some url";
                string lMyAccountPath = "some url";

                SNDCommonMailParameters lSNDCommonMailParameters = new SNDCommonMailParameters();
                string strSNDURL = System.Configuration.ConfigurationManager.AppSettings["SNDSiteUrl"];
                lSNDCommonMailParameters.MyAccountLink = string.Format("{0}://{1}{2}{3}", strhttp, strSNDURL, HttpContext.Current.Request.ApplicationPath, lMyAccountPath);
                lSNDCommonMailParameters.ContactUsLink = string.Format("{0}://{1}{2}{3}", strhttp, strSNDURL, HttpContext.Current.Request.ApplicationPath, lContactUsPath);
                lSNDCommonMailParameters.Username = (pContact.Account.aspnet_Users.UserName);

                string lFAQPath = "some url";
                lSNDCommonMailParameters.FAQLink = string.Format("{0}://{1}{2}{3}", strhttp, strSNDURL, HttpContext.Current.Request.ApplicationPath, lFAQPath);

                NotificationManager notificationManager =
               new NotificationManager(enumNotificationType.Online_account_creation, enumNotificationCategory.Account_Service, lSerContact, lSNDCommonMailParameters);
                return notificationManager.SendNotification((pContact.Email));
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("ClassName", "NavBase");
                    ex.Data.Add("MethodName", "SendSignUpConfirmationMail");
                    ExceptionHandler.handle(ex);
                    return false;
                }
                else
                { throw ex; }
            }
        }

        ///<summary>
        /// Method Name: SendSignUpConfirmationMail.
        /// Method Purpose: This method used to send signup confirmation mail.
        /// Author: Mena Armanyous.
        /// Modification Date: May 08, 2011.
        /// </summary>
        /// <param name="pCard">Card</param>
        /// <param name="pContact">Contact</param>       
        public bool SendCardRegistrationConfirmationMail(bool pIsSecureConnection = false)
        {
            try
            {
                Contact pContact = _NavigationState.Contact;
                //forming the notification mail then sent
                EDM.PrimitivePOCO.PrimitiveCard lCard = new EDM.PrimitivePOCO.PrimitiveCard(this._NavigationState.RegisteredCard);


                EDM.PrimitivePOCO.SNDCommonMailParameters lCommonMail = new EDM.PrimitivePOCO.SNDCommonMailParameters();
                // fix defect # 1346
                //set aspnet_users for account
                string lUserName = SessionManager.Get<string>(enumSessionKeys.UserName) ?? string.Empty;
                if (string.IsNullOrEmpty(lUserName) && pContact != null && pContact.Account != null)
                {
                    if (pContact.Account.aspnet_Users == null)
                    {
                        pContact.Account.aspnet_Users = new AccountAdministrationManager().GetAspNetUser(pContact.Account.aspnet_User_ID);
                    }
                    if (pContact.Account.aspnet_Users != null)
                    {
                        lUserName = (pContact.Account.aspnet_Users.UserName);
                    }
                }
                lCommonMail.Username = lUserName;
                // end defect # 1346 
                string strhttp = "http";
                if (HttpContext.Current.Request.IsSecureConnection == true)
                { strhttp += "s"; }
                string strSNDURL = System.Configuration.ConfigurationManager.AppSettings["SNDSiteUrl"];

                string lFAQPath = "some url";
                lCommonMail.FAQLink = string.Format("{0}://{1}{2}{3}", strhttp, strSNDURL, HttpContext.Current.Request.ApplicationPath, lFAQPath);
                EDM.PrimitivePOCO.PrimitiveContact lContact = new EDM.PrimitivePOCO.PrimitiveContact(pContact);

                NotificationManager notificationManager =
                new NotificationManager(enumNotificationType.Card_Registration, enumNotificationCategory.Account_Service, lContact, lCommonMail, lCard);
                //#Shady 14-5-2012 to get the email of the logged in user and to send the email to him              
                Contact emailContact = AccountAdministrationManager.GetContactByUserName(lUserName);
                return notificationManager.SendNotification(emailContact.Email);
                //if (notificationManager.SendNotification(HttpUtility.HtmlEncode(pContact.Email)))
                //{
                //    if (pDiscardNavigationToConfirmationPage == false)
                //    { DisplayConfirmationMessage(Resources.EmailNotification.SentEmailSuccess); }                   
                //}
                //else
                //{
                //    if (pDiscardNavigationToConfirmationPage == false)
                //    { DisplayConfirmationMessage(Resources.EmailNotification.SentEmailFailed); }                   
                //}
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("ClassName", "NavBase");
                    ex.Data.Add("MethodName", "SendCardRegistrationConfirmationMail");
                    ExceptionHandler.handle(ex);
                }
                else
                { throw ex; }
                return false;
            }
        }

        /// <summary>
        /// Method Name: SaveCardRegisteration
        /// Method Description: save the registered card to database
        /// </summary>
        /// <param name="pCard">Card</param>
        /// <returns>true for sucess, false for failure</returns>
        /// <Author> Mena Armanyous</Author>
        /// <Modified on>27-Apr-2011</Modified>
        public bool SaveCardRegisteration()
        {
            try
            {
                Card lCard = this._NavigationState.RegisteredCard;
                if (SessionManager.Contains(enumSessionKeys.AccountUserID))
                    lCard.CardRegistered.Account_ID = SessionManager.Get<int>(enumSessionKeys.AccountUserID);
                else if (NavigationState.Contact.Contact_ID != 0)
                    lCard.CardRegistered.Account_ID = NavigationState.Contact.Contact_ID;
                else
                    lCard.CardRegistered.Account_ID = AccountAdministrationManager.GetCurrentUserAccount().Account_ID;
                lCard.IsRegistered = true;
                lCard.ModifiedBy = SessionManager.Get<string>(enumSessionKeys.UserName);

                return _CardManager.RegisterCard(lCard);
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("ClassName", "NavBase");
                    ex.Data.Add("MethodName", "SaveCardRegisteration");
                    ExceptionHandler.LogException(ex);
                }
                else
                { throw ex; }
                return false;
            }
        }

        public void CloneContact(Contact pSource, Contact pTarget)
        {
            pTarget.DataOfBirth = pSource.DataOfBirth;
            pTarget.Email = pSource.Email;
            pTarget.FirstName = pSource.FirstName;
            pTarget.MiddleInitial = pSource.MiddleInitial;
            pTarget.Mobile = pSource.Mobile;
            pTarget.Phone = pSource.Phone;
            pTarget.Surname = pSource.Surname;
            pTarget.Title = pSource.Title;

            if (pSource.Addresses.Count > 0)
            {
                Address add;
                if (pTarget.Addresses.Count > 0)
                    add = pTarget.Addresses.FirstOrDefault();
                else
                    add = new Address();

                Address source = pSource.Addresses.FirstOrDefault();
                add.AddressLine1 = source.AddressLine1;
                add.AddressLine2 = source.AddressLine2;
                add.AddressLine3 = source.AddressLine3;
                add.Country_ID = source.Country_ID;
                add.County = source.County;
                add.Town = source.Town;
                add.FirstName = source.FirstName;
                add.SC_CountryCode = source.SC_CountryCode;

                if (pTarget.Addresses.Count == 0)
                    pTarget.Addresses.Add(add);
            }
        }

        public bool IsCardProfileValidForRegistration(string pCardNumber)
        {
            try
            {
                CardManager lCardMgr = new CardManager();

                int lProfileCode = lCardMgr.GetCardProfileCode(pCardNumber);

                if (lProfileCode == SWC.SC_CardProfile.Tourist || lProfileCode == SWC.SC_CardProfile.Trainee)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("PageName", "CardRegisteration");
                    ex.Data.Add("EventName", "IsCardProfileValidForRegistration");
                    ExceptionHandler.handle(ex);
                }
                else
                { throw ex; }
            }

            return true;
        }

        #endregion       
      
        #region Public Static Methods
        /// <summary>
        /// Method Name: ClearAllStates.
        /// Method Purpose: This method used to clear all navigation states from session.
        /// Author: Mena Armanyous.
        /// Modification Date: May 02, 2011.
        /// </summary>
        public static void ClearAllStates() 
        {
            if (SessionManager.Contains(enumSessionKeys.NavCardPruchase) == true)
            { SessionManager.Remove(enumSessionKeys.NavCardPruchase); }

            if (SessionManager.Contains(enumSessionKeys.NavCardTopup) == true)
            { SessionManager.Remove(enumSessionKeys.NavCardTopup); }

            if (SessionManager.Contains(enumSessionKeys.NavCardRegistration))
            { SessionManager.Remove(enumSessionKeys.NavCardRegistration); }
        }
        #endregion

    }
}
