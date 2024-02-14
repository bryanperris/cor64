using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;

namespace RunN64.Graphics
{
    public class GfxCursor
    {
        private Vector2 m_Origin;
        private Vector2 m_Cursor;
        private float m_Spacing;

        private Vector2 m_LastSize;

        internal GfxCursor(Vector2 origin) {
            m_Origin = origin;
            m_Cursor = origin;
        }

        public GfxCursor Size(float w, float h) {
            m_Cursor.X += w;
            m_Cursor.Y += h;
            return this;
        }

        public GfxCursor SizeY(float size) {
            m_Cursor.Y += size;
            return this;
        }

        public GfxCursor SizeX(float size) {
            m_Cursor.X += size;
            return this;
        }

        public GfxCursor MoveRight() {
            m_Cursor.X += m_Spacing;
            m_Cursor.Y = m_Origin.Y;
            return this;
        }

        // public GfxCursor RepeatSize() {
        //     return MoveSize(m_LastSize.X, m_LastSize.Y);
        // }

        public GfxCursor Spacing(float space) {
            m_Spacing = space;
            return this;
        }


        public GfxCursor Clone() {
            return new GfxCursor(this);
        }


        public Vector2 Position => m_Cursor;

        public static implicit operator Vector2(GfxCursor cursor)
        {
            return cursor.m_Cursor;
        }
    }
}