using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using SND.BusinessManagers.Domain;
using SND.EDM;
using SND.Enums;
using SND.BusinessManagers.Utilities;
using System.Configuration;
using System.Web.Profile;



namespace SND.UI.BusinessNavigators
{
    public class NavigationState
    {
        #region Private Members
        private Guid _LoggedInUserGUID = Guid.Empty; // Private member used to hold current logged on user related GUID.
        private string _LoggedInUserName = string.Empty; // Private member used to hold current logged on user name.
        private Card _Card;// Private member used to hold registered card object.
        private enumLoginAction _LoginAction = enumLoginAction.Discard; //Private member used to hold login action.
        private Contact _Contact = null;
        private bool _NavigationPermitted = true; //Private member used to hold if navigation process permitted or not.
        private enumNavigationStep _NavigationStep = enumNavigationStep.StepHome; //Private member used to hold current navigation step number.
        private enumNavigationStrategyType _NavigationStrategyType = enumNavigationStrategyType.None; //Private member used to hold current navigation type.
        private SB_Card _SB_Card = new SB_Card();
        private SB_ShoppingBasket _SB_ShoppingBasket = null;
        private int _RegisteredCardID = 0;
        private enumTopupAction _TopupAction;
        private bool _RegisterAnonymousCardWhileTopUp = false;
        private bool _ContinueTopupWithoutRegisterCard = false;
        private bool _CardRegistrationCompletedFailed = false;
        private bool _RegistrationProcessCompleted = false;
        private string _RegistrationResultMsg = string.Empty;
        private SB_Card _ReplaceCard = null;
        private bool _HomeNavigation = false;
        private bool _AutoTopupChecked = false;
        private bool _ScenarioCompleted = false;
        private Contact _TempCardHolderContact;

        #endregion

        /// <summary>
        /// Method Name: NavigationState.
        /// Method Purpose:  Public default contracture.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011. 
        /// </summary>
        public NavigationState() { }

        public SB_Card SB_Card
        {
            get { return _SB_Card; }
            set { _SB_Card = value; }
        }


        public SB_ShoppingBasket SB_ShoppingBasket
        {
            get { return _SB_ShoppingBasket; }
            set { _SB_ShoppingBasket = value; }
        }

        /// <summary>
        /// Property Name: Contact.
        /// Method Purpose: This property used to set & get current contact object.
        /// Author: Mena Armanyous.
        /// Modification Date: April 26, 2011. 
        /// </summary>
        public Contact Contact
        {
            get { return this._Contact; }
            set { this._Contact = value; }
        }

        /// <summary>
        /// Method Name: NavigationState.
        /// Method Purpose:  Public custom contracture used to pass current logged on user related GUID.
        /// Author: Mena Armanyous.
        /// Modification Date: April 26, 2011. 
        /// </summary>
        public NavigationState(Guid pLoggedInUserGUID, string pLoggedInUserName)
        {
            _LoggedInUserGUID = pLoggedInUserGUID;
            _LoggedInUserName = pLoggedInUserName;
        }

        /// <summary>
        /// Property Name: LoggedInUserGUID.
        /// Method Purpose: This property used to set & get current logged on user related GUID.
        /// Author: Mena Armanyous.
        /// Modification Date: April 26, 2011. 
        /// </summary>
        public Guid LoggedInUserGUID
        {
            get { return this._LoggedInUserGUID; }
            set { this._LoggedInUserGUID = value; }
        }

        /// <summary>
        /// Property Name: LoggedInUserName.
        /// Method Purpose: This property used to set & get current logged on user name.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011. 
        /// </summary>
        public string LoggedInUserName
        {
            get { return this._LoggedInUserName; }
            set { this._LoggedInUserName = value; }
        }



        /// <summary>
        /// Property Name: LoginAction.
        /// Method Purpose: This property used to set & get current log action.
        /// Author: Mena Armanyous.
        /// Modification Date: April 26, 2011. 
        /// </summary>
        public enumLoginAction LoginAction
        {
            get { return this._LoginAction; }
            set { this._LoginAction = value; }
        }

        /// <summary>
        /// Property Name: NavigationStep.
        /// Method Purpose: This property used to set & get navigation step.
        /// Author: Mena Armanyous.
        /// Modification Date: April 26, 2011. 
        /// </summary>
        public enumNavigationStep NavigationStep
        {
            get { return this._NavigationStep; }
            set { this._NavigationStep = value; }
        }

        /// <summary>
        /// Property Name: NavigationStrategyType.
        /// Method Purpose: This property used to set & get navigation type.
        /// Author: Mena Armanyous.
        /// Modification Date: April 26, 2011. 
        /// </summary>
        public enumNavigationStrategyType NavigationStrategyType
        {
            get { return this._NavigationStrategyType; }
            set { this._NavigationStrategyType = value; }
        }


        /// <summary>
        /// Property Name: NavigationPermitted
        /// Method Purpose: This property used to set & get if navigation permitted or not.
        /// Author: Mena Armanyous.
        /// Modification Date: May 05, 2011. 
        /// </summary>
        public bool NavigationPermitted
        {
            get { return this._NavigationPermitted; }
            set { this._NavigationPermitted = value; }
        }

        /// <summary>
        /// Property Name: RegisteredCard.
        /// Property Purpose: This property used to set & get the Card to be Registered.
        /// Author: Shady Yahia.
        /// Modification Date: April 27, 2011. 
        /// </summary>
        public Card RegisteredCard
        {
            get { return this._Card; }
            set { this._Card = value; }
        }

        
        private bool _RegisterationCompletedSuccessfully = false;
        public bool RegisterationCompletedSuccessfully
        {
            get { return this._RegisterationCompletedSuccessfully; }
            set { this._RegisterationCompletedSuccessfully = value; }
        }

        /// <summary>
        /// Property Name: RegisteredCardID.
        /// Method Purpose: This property used to set & get registered card id.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011. 
        /// </summary>
        public int RegisteredCardID
        {
            get { return this._RegisteredCardID; }
            set { this._RegisteredCardID = value; }
        }

        /// <summary>
        /// Property Name: RegisterAnonymousCardWhileTopUp
        /// Method Purpose: This property used to set & get if we will register anonymous card while Top-Up.
        /// Author: Mena Armanyous.
        /// Modification Date: May 03, 2011. 
        /// </summary>
        public bool RegisterAnonymousCardWhileTopUp
        {
            get { return this._RegisterAnonymousCardWhileTopUp; }
            set { this._RegisterAnonymousCardWhileTopUp = value; }
        }

        public bool RegistrationProcessCompleted
        {
            get { return _RegistrationProcessCompleted; }
            set { _RegistrationProcessCompleted = value; }
        }

        /// <summary>
        /// Property Name: TopupAction
        /// Method Purpose: This property used to set & get Top-Up action.
        /// Author: Mena Armanyous.
        /// Modification Date: April 27, 2011. 
        /// </summary>
        public enumTopupAction TopupAction
        {
            get { return this._TopupAction; }
            set { this._TopupAction = value; }
        }

        /// <summary>
        /// Property Name: CardRegistrationCompletedSuccessfully
        /// Method Purpose: This property used to set & get if card registration completed successfully.
        /// Author: Mena Armanyous.
        /// Modification Date: May 09, 2011. 
        /// </summary>
        public bool CardRegistrationCompletedFailed
        {
            get { return this._CardRegistrationCompletedFailed; }
            set { this._CardRegistrationCompletedFailed = value; }
        }
        /// <summary>
        /// Property Name: ContinueTopupWithoutRegisterCard 
        /// Method Purpose: This property used to set & get if we can continue Top-Up without register card.
        /// Author: Mena Armanyous.
        /// Modification Date: May 09, 2011. 
        /// </summary>
        public bool ContinueTopupWithoutRegisterCard
        {
            get { return this._ContinueTopupWithoutRegisterCard; }
            set { this._ContinueTopupWithoutRegisterCard = value; }
        }

        public string RegistrationResultMsg
        {
            get { return _RegistrationResultMsg; }
            set { _RegistrationResultMsg = value; }
        }

        public bool HomeNavigation
        {
            get { return _HomeNavigation; }
            set { _HomeNavigation = value; }
        }

        public SB_Card ReplaceCard
        {
            get { return _ReplaceCard; }
            set { _ReplaceCard = value; }
        }

        public bool AutoTopupChecked
        {
            get { return _AutoTopupChecked; }
            set { _AutoTopupChecked = value; }
        }

        public Contact TempCardHolderContact
        {
            get { return _TempCardHolderContact; }
            set { _TempCardHolderContact = value; }
        }

        public bool ScenarioCompleted
        {
            get { return _ScenarioCompleted; }
            set { _ScenarioCompleted = value; }
        }
    }
}
