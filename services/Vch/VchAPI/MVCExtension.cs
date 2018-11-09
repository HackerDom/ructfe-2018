using Microsoft.AspNetCore.Mvc;

namespace VchAPI
{
    public static class MVCExtension
    {
        public static ActionResult<TValue> ToActionResult<TValue>(this TValue source) where TValue : class
        {
            return new ActionResult<TValue>(source);
        }
    }
}