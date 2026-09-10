using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertExplorer
{
    public static class Vector2Extensions
    {
        public static Vector2i ToVector2i(this Vector2s vec2s)
        {
            return new Vector2i(vec2s.x, vec2s.y);
        }

        public static Vector2s ToVector2s(this Vector2i vec2i)
        {
            return new Vector2s(vec2i.x, vec2i.y);
        }
    }
}
