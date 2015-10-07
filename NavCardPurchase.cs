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
using SND.BusinessManagers.Utilities.UrlLocalization;

namespace SND.UI.BusinessNavigators
{
    public class NavCardPurchase : NavBase
    {        
        #region private members

        private CardManager _CardManager = new CardManager();
        SchemeCodesManager lSchemeCodesManager = new SchemeCodesManager();

        #endregion

        #region public members

        /// <summary>
        /// Method Name: CardPurchaseNavigation. Method Purpose:  Public default
        /// constructor that initiates the local variables. 
        /// Author: Mena Armanyous.
        /// Modification Date: April 28, 2011.
        /// </summary>
        /// <param name="pCreateNewNavigationState"></param>
        public NavCardPurchase(bool pCreateNewNavigationState = false)
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
        /// Property Name: CardPurchaseNavigationState.
        /// Method Purpose: Get navigation state object.
        /// Author: Mena Armanyous.
        /// Modification Date: April 28, 2011.
        /// </summary>
        public override NavigationState NavigationState
        {
            get
            {
                if (SessionManager.Contains(enumSessionKeys.NavCardPruchase) == true)
                { base._NavigationState = SessionManager.Get<NavigationState>(enumSessionKeys.NavCardPruchase); }
                return base._NavigationState as NavigationState;
            }
        }

        /// <summary>
        /// Method Name: CancelNavigation. Method Purpose: Cancel navigation. Author: Mena
        /// Armanyous. Modification Date: April 28, 2011.
        /// </summary>
        public override void CancelNavigation()
        {
            try
            {
                string lRedirectUrl = "~/Home.aspx";
                if (_NavigationState.NavigationStep == enumNavigationStep.StepSignUp)
                { lRedirectUrl = "SOME URL1"; }
                HttpContext.Current.Response.Redirect(lRedirectUrl);
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("CardPurchaseNavigation", "CancelNavigation");
                    ExceptionHandler.handle(ex);
                }
                else
                { throw ex; }
            }
        }

        /// <summary>
        /// Method Name: GetAllLists. Method Purpose: Get all lists for some scenario
        /// Author: Mena Armanyous. Modification Date: April 28, 2011.
        /// </summary>
        /// <returns>Return 'Hashtable' object used to hold all lists related to current
        /// navigation step.</returns>
        public override System.Collections.Hashtable GetAllLists()
        {

            Hashtable lAllLists = new Hashtable();

            try
            {
                //Add the lists to the hash table
                IList lCardProfileList = GetList(enumListType.CardProfileList);
                if (lCardProfileList != null)
                {
                    lAllLists.Add(enumListType.CardProfileList, GetList(enumListType.CardProfileList));
                }

                IList lConfiguredTopupList = GetList(enumListType.ConfiguredTopUpList);

                if (lConfiguredTopupList != null)
                {
                    lAllLists.Add(enumListType.ConfiguredTopUpList, GetList(enumListType.ConfiguredTopUpList));
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("CardPurchaseNavigation", "GetAllLists");
                    ExceptionHandler.handle(ex);
                }
                else
                {
                    throw ex;
                }
            }

            return lAllLists;
        }

        /// <summary>
        /// Method Name: GetList. Method Purpose: Get specific list type from SND Web
        /// Portal database. Author: Mena Armanyous. Modification Date: April 28, 2011.
        /// </summary>
        /// <returns>Return IList object used to hold the requested list type.</returns>
        /// <param name="pListType"></param>
        public override System.Collections.IList GetList(enumListType pListType)
        {

            try
            {
                switch (pListType)
                {
                    case enumListType.ConfiguredTopUpList:
                        return SBConfigValues.ListCredit;
                    case enumListType.CardProfileList:
                        ICollection<SC_CardProfile> lCardProfileList;
                        lCardProfileList = lSchemeCodesManager.GetSchemeCode<SC_CardProfile>();
                        return lCardProfileList.ToList();
                    default:
                        return SearchAllLists(pListType);
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("CardPurchaseNavigation", "GetList");
                    ExceptionHandler.LogException(ex);
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }
       
        /// <summary>
        /// Method Name: GetNextNavigationStep.
        /// Method Purpose: Get next navigation step number.
        /// Author: Mena Armanyous.
        /// Modification Date: April 28, 2011.
        /// </summary>
        /// <returns>Return next navigation step number.</returns>
        protected override enumNavigationStep GetNextNavigationStep()
        {
            try
            {
                enumNavigationStep lStep = enumNavigationStep.None;
                switch (_NavigationState.NavigationStep)
                {
                    case enumNavigationStep.StepHome:
                        return enumNavigationStep.StepCardPurchase;

                    case enumNavigationStep.StepCardPurchase:

                        if (_NavigationState.LoggedInUserGUID != Guid.Empty)
                        {
                            if (NavigationState.SB_Card.CardProfileCode == SWC.SC_CardProfile.PersonalizedChild)
                            {
                                lStep = enumNavigationStep.StepChildCardDetails;
                            }
                            else
                            {
                                lStep = enumNavigationStep.StepAddCard;
                            }
                        }
                        else if (NavigationState.SB_Card.CardProfileCode == SWC.SC_CardProfile.PersonalizedChild)
                        {
                            lStep = enumNavigationStep.StepChildLoginSelection;
                        }
                        else
                        {
                            lStep = enumNavigationStep.StepLoginSelection;
                        }
                        break;
                    case enumNavigationStep.StepLoginSelection:
                        if (_NavigationState.LoginAction == enumLoginAction.Login)
                        {
                            lStep = enumNavigationStep.StepAddCard;
                        }
                        else if (_NavigationState.LoginAction == enumLoginAction.Discard)
                        {
                            lStep = enumNavigationStep.StepAddCard;
                        }
                        else if (NavigationState.LoginAction == enumLoginAction.Signup)
                        {
                            lStep = enumNavigationStep.StepSignUp;
                        }
                        break;

                     case enumNavigationStep.StepChildLoginSelection:
                        if (_NavigationState.LoginAction == enumLoginAction.Login)
                        {
                            if(new SND.BusinessManagers.Domain.ConfigurationManager().IsChildPersonalizedVerificationProcessEnabled()
                                && _NavigationState.SB_ShoppingBasket.SB_Cards.Count > 0)
                                lStep = enumNavigationStep.StepCardPurchase;
                            else                            
                                lStep = enumNavigationStep.StepChildCardDetails;
                        }
                        else if (NavigationState.LoginAction == enumLoginAction.Signup)
                        {
                            lStep = enumNavigationStep.StepSignUp;
                        }
                        break;                

                    case enumNavigationStep.StepSignUp:
                        lStep = enumNavigationStep.StepSecurityInformation;
                        break;

                    case enumNavigationStep.StepSecurityInformation:
                        if (NavigationState.SB_Card.CardProfileCode == SWC.SC_CardProfile.PersonalizedChild)
                        {
                            lStep = enumNavigationStep.StepChildCardDetails;
                        }
                        else
                        {
                            lStep = enumNavigationStep.StepAddCard;
                        }
                        break;

                    case enumNavigationStep.StepChildCardDetails:
                        lStep = enumNavigationStep.StepChildCardGuardian;
                        break;
                    case enumNavigationStep.StepChildCardGuardian:
                        lStep = enumNavigationStep.StepAddCard;
                        break;
                }
                return lStep;
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("CardPurchaseNavigation", "GetNextNavigationStep");
                    ExceptionHandler.LogException(ex);
                }
                else
                {
                    throw ex;
                }
            }
            return enumNavigationStep.None;
        }

        /// <summary>
        /// Method Name: GetNextNavigationStepURL. Method Purpose: Get next navigation step
        /// related URL. Author: Mena Armanyous. Modification Date: April 28, 2011.
        /// </summary>
        /// <param name="pNextNavigationStep"></param>
        protected override string GetNextNavigationStepURL(enumNavigationStep pNextNavigationStep)
        {
            try
            {
                switch (pNextNavigationStep)
                {
                    case enumNavigationStep.StepCardPurchase:
                        return "SOME URL";

                    case enumNavigationStep.StepLoginSelection:
                        return "SOME URL";

                    case enumNavigationStep.StepContactInfomration:
                        return "SOME URL";

                    case enumNavigationStep.StepSignUp:
                        return "SOME URL";

                    case enumNavigationStep.StepSecurityInformation:
                        return "SOME URL";

                    case enumNavigationStep.StepAddCard:
                        return "SOME URL";

                    case enumNavigationStep.StepChildLoginSelection:
                        return "SOME URL";

                    case enumNavigationStep.StepChildCardGuardian:
                        return "SOME URL";

                    case enumNavigationStep.StepChildCardDetails:
                        return "SOME URL";

                    case enumNavigationStep.StepHome:
                        return "~/Home.aspx";
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("CardPurchaseNavigation", "GetNextNavigationStepURL");
                    ExceptionHandler.LogException(ex);
                }
                else
                { throw ex; }
            }
            return string.Empty;
        }

        /// <summary>
        /// Method Name: NavigateToNextStep. Method Purpose: Navigation to next step.
        /// Author: Mena Armanyous. Modification Date: April 28, 2011.
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
                    string lNextPageURL;

                    //get the next step
                    _NavigationState.NavigationStep = GetNextNavigationStep();

                    //check that it's not none
                    if (_NavigationState.NavigationStep != enumNavigationStep.None)
                    {
                        if (_NavigationState.NavigationStep == enumNavigationStep.StepAddCard)
                        {
                            UpdateSBCollection();
                        }

                        //get the next page URL
                        lNextPageURL = GetNextNavigationStepURL(_NavigationState.NavigationStep);

                        //store the local navigation state object in session
                        SetNavigationState();

                        //go to the next page
                        if (!lNextPageURL.Equals(string.Empty))
                        { HttpContext.Current.Response.Redirect(lNextPageURL); }
                        else
                        { HttpContext.Current.Response.Redirect("~/FileNotFound.aspx"); }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("CardPurchaseNavigation", "NavigateToNextStep");
                    ExceptionHandler.handle(ex);
                }
                else
                { throw ex; }
            }
        }

        /// <summary>
        /// Method Name: StartNavigation. Method Purpose: Start navigation. should be
        /// called from the staging page Author: Mena Armanyous. Modification Date: April
        /// 28, 2011.
        /// </summary>
        public override void StartNavigation()
        {
            try
            {
                NavigationState.NavigationStep = enumNavigationStep.StepHome;

                NavigateToNextStep();

            }
            catch (Exception ex)
            {
                if (!ExceptionHandler.IsUserFriendly(ex))
                {
                    ex.Data.Add("CardPurchaseNavigation", "StartNavigation");
                    ExceptionHandler.handle(ex);
                }
                else
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// CR119-08
        /// Save the contact and address objects extarcted from the card personaliztion object so it can be used later to populate tha anonymous user data
        /// </summary>
        public void SavePersonalizedContact()
        {

            OnlineAccountsManager lOnlineAccMgr = new OnlineAccountsManager();
            Contact lTargetContact = null;

            if (HttpContext.Current.Request.Cookies["PersonalizedContact"] != null)
            {
                //modified by Rabie @ 29 Oct 2013
                // decrypting "PersonalizedContact" cookie value as part of accepted work-around to "persistent cookie issue" in SND 4.0 security issues report
                var lPersonalizedContactID = EncryptionDecryptionManager.DecryptData(HttpContext.Current.Request.Cookies["PersonalizedContact"].Value
                    , URLLocalizationManager.PrivateKey);
                lTargetContact = lOnlineAccMgr.GetContact(int.Parse(lPersonalizedContactID), "Addresses");
                ConvertPersonalizationDataToContact(NavigationState.SB_Card.SB_CardPersonalizedInfo, lTargetContact);
                lOnlineAccMgr.UpdateContact(lTargetContact);
                lOnlineAccMgr.UpdateAddress(lTargetContact.Addresses.FirstOrDefault());
            }
            else
            {
                lTargetContact = new Contact();
                ConvertPersonalizationDataToContact(NavigationState.SB_Card.SB_CardPersonalizedInfo, lTargetContact);
                lOnlineAccMgr.CreateContact(lTargetContact);
            }
            //modified by Rabie @ 29 Oct 2013
            // encrypting/decrypting "PersonalizedContact" cookie value as accepted work-around to "persistent cookie issue" in SND 4.0 security issues report
            var lPersonalizedContactCookie = new HttpCookie("PersonalizedContact",
                EncryptionDecryptionManager.EncryptData(lTargetContact.Contact_ID.ToString(), URLLocalizationManager.PublicKey));

            lPersonalizedContactCookie.Expires = DateTime.Now.AddDays(1);
            lPersonalizedContactCookie.HttpOnly = true;
            lPersonalizedContactCookie.Secure = true;
            HttpContext.Current.Response.Cookies.Add(lPersonalizedContactCookie);
        }

        #endregion

        #region private methods

        protected override void SetNavigationState()
        {
            try
            {
                if (_NavigationState != null)
                {
                    SessionManager.Set<NavigationState>(enumSessionKeys.NavCardPruchase, this.NavigationState);
                }
                else
                {
                    _NavigationState = new NavigationState();
                    SessionManager.Set<NavigationState>(enumSessionKeys.NavCardPruchase, _NavigationState);
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
        /// CR119-08
        /// extract contact and address objects from the card personalization object
        /// </summary>
        /// <param name="pCardPersonalizedInfo"></param>
        /// <returns></returns>
        private Contact ConvertPersonalizationDataToContact(SB_CardPersonalizedInfo pCardPersonalizedInfo, Contact pContact)
        {

            pContact.FirstName = pCardPersonalizedInfo.HolderForename;
            pContact.Surname = pCardPersonalizedInfo.HolderSurname;
            pContact.DataOfBirth = pCardPersonalizedInfo.HolderDateOfBirth;
            pContact.Email = pCardPersonalizedInfo.HolderEmailAddress;
            pContact.Mobile = pCardPersonalizedInfo.HolderMobile;
            pContact.Phone = pCardPersonalizedInfo.HolderTelephone;

            if (pContact.Addresses.Count == 0)
            {
                pContact.Addresses.Add(new Address()
                {
                    AddressLine1 = pCardPersonalizedInfo.HolderAddressLine1,
                    AddressLine2 = pCardPersonalizedInfo.HolderAddressLine2,
                    AddressLine3 = pCardPersonalizedInfo.HolderAddressLine3,
                    County = pCardPersonalizedInfo.HolderAddressCounty,
                    Town = pCardPersonalizedInfo.HolderAddressTown,
                    Country_ID = pCardPersonalizedInfo.SC_CountryCode_ID
                });
            }
            else
            {
                Address lAddress = pContact.Addresses.FirstOrDefault();
                lAddress.AddressLine1 = pCardPersonalizedInfo.HolderAddressLine1;
                lAddress.AddressLine2 = pCardPersonalizedInfo.HolderAddressLine2;
                lAddress.AddressLine3 = pCardPersonalizedInfo.HolderAddressLine3;
                lAddress.County = pCardPersonalizedInfo.HolderAddressCounty;
                lAddress.Town = pCardPersonalizedInfo.HolderAddressTown;
                lAddress.Country_ID = pCardPersonalizedInfo.SC_CountryCode_ID;
            }

            return pContact;
        }

        private void SetNavigationStateLocalMember()
        {
            try
            {
                this.NavigationState.SB_Card = new SB_Card();
                this.NavigationState.SB_Card.IsNew = true;

                this.NavigationState.NavigationStrategyType = enumNavigationStrategyType.CardPurchaseStrategy;

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
    }
}
