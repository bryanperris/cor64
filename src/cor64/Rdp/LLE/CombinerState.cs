using System;

namespace cor64.Rdp.LLE {
        public class ConstantColorRef : ColorRef {
            public ConstantColorRef(Color color) : base(color) {

            }

            public override void SetColor(Color color) {

            }

            public override void SetRGBFromSingle(int value) {

            }

            public override int R {
                get => color.R;
                set {}
            }

            public override int G {
                get => color.G;
                set {}
            }

            public override int B {
                get => color.B;
                set {}
            }

            public override int A {
                get => color.A;
                set {}
            }
        }

        public class CombinerInputRef {
            private ColorRef m_RgbRef = ONE;
            private ColorRef m_AlphaRef = ONE;

            public static readonly ConstantColorRef ZERO = new ConstantColorRef(Color.Zero);
            public static readonly ConstantColorRef ONE = new ConstantColorRef(Color.One);

            public string ColorTag { get; private set; }

            public string AlphaTag { get; private set; }

            public void SelectRgb(ColorRef r, String tag = "") {
                m_RgbRef = r;
                ColorTag = tag;
            }

            public void SelectAlpha(ColorRef r, String tag = "") {
                m_AlphaRef = r;
                AlphaTag = tag;
            }

            public void SetRgbZero() {
                m_RgbRef = ZERO;
            }

            public void SetAlphaZero() {
                m_AlphaRef = ZERO;
            }

            public void SetRgbOne() {
                m_RgbRef = ONE;
            }

            public void SetAlphaOne() {
                m_AlphaRef = ONE;
            }

            public bool IsColorRefEqual(ColorRef cr) {
                return ReferenceEquals(m_RgbRef.GetSource(), cr);
            }

            public bool IsAlphaRefEqual(ColorRef cr) {
                return ReferenceEquals(m_AlphaRef.GetSource(), cr);
            }

            public int R {
                get => m_RgbRef.R;
                set => m_RgbRef.R = value;
            }

            public int G {
                get => m_RgbRef.G;
                set => m_RgbRef.G = value;
            }
            public int B {
                get => m_RgbRef.B;
                set => m_RgbRef.B = value;
            }

            public int A {
                get => m_AlphaRef.A;
                set => m_AlphaRef.A = value;
            }

            public Color GetColor() => new Color(R, G, B, A);
        }

        public class CombinerState {
            public readonly CombinerInputRef SubtractA = new CombinerInputRef();
            public readonly CombinerInputRef SubtractB = new CombinerInputRef();
            public readonly CombinerInputRef Multiply = new CombinerInputRef();
            public readonly CombinerInputRef Add = new CombinerInputRef();
            public CombinerInputColor InputSubtractA;
            public CombinerInputColor InputSubtractB;
            public CombinerInputColor InputMultiply;
            public CombinerInputColor InputAdd;

            public void SelectAll(int selection) {
                InputSubtractA.RGBSelection = selection;
                InputSubtractB.RGBSelection = selection;
                InputMultiply.RGBSelection = selection;
                InputAdd.RGBSelection = selection;

                InputSubtractA.AlphaSelection = selection;
                InputSubtractB.AlphaSelection = selection;
                InputMultiply.AlphaSelection = selection;
                InputAdd.AlphaSelection = selection;
            }
        }

        public struct CombinerInputColor {
            public int RGBSelection;
            public int AlphaSelection;
        }
}