namespace GameInput
{
    public static class Inputs
    {
        public static InputActions Input
        {
            get
            {
                if (_input != null)
                    return _input;

                _input = new InputActions();

                return _input;
            }
        }
        private static InputActions _input;
    }
}
