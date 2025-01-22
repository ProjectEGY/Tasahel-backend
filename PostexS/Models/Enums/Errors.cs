using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Enums
{
    public enum Errors
    {
        Success,
        TheModelIsInvalid,
        SomeThingWentwrong,
        TheUserNotExistOrDeleted,
        UserIsDeleted,
        UserIsPending,
        UserIsRejected,
        ThisPhoneNumberAlreadyExist,
        ThisPhoneNumberNotExist,
        TheUsernameOrPasswordIsIncorrect,
        TheOldPasswordIsInCorrect,
        TheOrderNotExistOrDeleted,
        ThisOrderAssignedToAnotherAgent,
        PublicKeyIsRequired,
        PrivateKeyIsRequired,
        PublicKeyIsInvalid,
        PrivateKeyIsInvalid,
        PrivateKeyIsWrongOrPublicKeyIsWrong,
        //ReturnedImageIsRequired,
    }
}
