using System;


public static class EnumerationExtensions {

    public static bool Has<T>(this System.Enum type, T value) {
        try {
            return (((int)(object)type & (int)(object)value) == (int)(object)value);
        } 
        catch {
            return false;
        }
    }

    public static bool Is<T>(this System.Enum type, T value) {
        try {
            return (int)(object)type == (int)(object)value;
        }
        catch {
            return false;
        }    
    }


    public static T Add<T>(this System.Enum type, T value) {
        try {
            return (T)(object)(((int)(object)type | (int)(object)value));
        }
        catch(Exception ex) {
            throw new ArgumentException(
                string.Format(
                    "Could not append value from enumerated type '{0}'.",
                    typeof(T).Name
                    ), ex);
        }    
    }


    public static T Remove<T>(this System.Enum type, T value) {
        try {
            return (T)(object)(((int)(object)type & ~(int)(object)value));
        }
        catch (Exception ex) {
            throw new ArgumentException(
                string.Format(
                    "Could not remove value from enumerated type '{0}'.",
                    typeof(T).Name
                    ), ex);
        }  
    }

}
