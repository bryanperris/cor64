namespace cor64 {

    /// <summary>
    /// Attempts to bypass the cartridge IPL
    /// </summary>
    public abstract class DirectBoot {
        public abstract void Boot(Cartridge cartridge);
    }

    public class DirectBootX102 {
    }
}