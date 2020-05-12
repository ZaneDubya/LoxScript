namespace XPT {
    class TestNativeObject {
        public int TestInt = 1;
        public double TestDouble = 2.3;
        public string TestStr = "Hello";
        public string TestStrNull;

        public int TestProperty {
            get;
            set;
        }

        public void TestFn() {

        }

        public int TestOut() {
            return 1234567;
        }

        public void TestParam(int a, string b) {

        } 

        private void DontGetThis() {

        }

        protected void DontGetThisEitheR() {

        }

        internal void DontGetThisEither2() {

        }
    }
}
