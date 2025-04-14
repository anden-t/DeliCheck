namespace DeliCheck.Models
{
    /// <summary>
    /// Представляет результат регистрации
    /// </summary>
    public class RegisterResult
    {
        /// <summary>
        /// Пользователь с таким <see cref="UserModel.Username"/> <see cref="UserModel.PhoneNumber"/> <see cref="UserModel.Email"/> уже существует.
        /// </summary>
        public static readonly RegisterResult UserExist = new RegisterResult(RegisterResultState.UserExist);
        /// <summary>
        /// Указаны неправильные данные при регистрации
        /// </summary>
        public static readonly RegisterResult InvalidData = new RegisterResult(RegisterResultState.InvalidData);

        /// <summary>
        /// Создает результат с неуспешным <see cref="State"/>
        /// </summary>
        /// <param name="state">Состояние регистрации</param>
        private RegisterResult(RegisterResultState state)
        {
            State = state;
            User = null;
        }

        /// <summary>
        /// Создает успешный результат
        /// </summary>
        /// <param name="user">Зарегистрированный пользователь</param>
        public RegisterResult(UserModel user)
        {
            User = user;
            State = RegisterResultState.Success;
        }

        /// <summary>
        /// Зарегистрированный пользователь. <see cref="null"/>, если <see cref="State"/> != <see cref="RegisterResultState.Success"/>
        /// </summary>
        public UserModel? User { get; set; }
        /// <summary>
        /// Состояние регистрации
        /// </summary>
        public RegisterResultState State { get; set; }
    }
    /// <summary>
    /// Состояние регистрации
    /// </summary>
    public enum RegisterResultState
    {
        /// <summary>
        /// Успешно
        /// </summary>
        Success,
        /// <summary>
        /// Пользователь с таким <see cref="UserModel.Username"/> <see cref="UserModel.PhoneNumber"/> <see cref="UserModel.Email"/> уже существует.
        /// </summary>
        UserExist,
        /// <summary>
        /// Указаны неправильные данные при регистрации
        /// </summary>
        InvalidData
    }
}
