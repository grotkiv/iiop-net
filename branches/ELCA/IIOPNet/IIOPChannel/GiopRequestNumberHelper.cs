/* IIOPRequestNumberHelper.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 17.01.03  Dominic Ullmann (DUL), dul@elca.ch
 * 
 * Copyright 2003 Dominic Ullmann
 *
 * Copyright 2003 ELCA Informatique SA
 * Av. de la Harpe 22-24, 1000 Lausanne 13, Switzerland
 * www.elca.ch
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;

namespace Ch.Elca.Iiop {

    /// <summary>
    /// this class is able to generate unique request id for a connection
    /// TODO: handle overflow correctly
    /// </summary>
    public class GiopRequestNumberGenerator {

        #region IFields

        private uint m_last = 5;

        #endregion IFields
        #region IConstructors
        
        public GiopRequestNumberGenerator() {
        }

        #endregion IConstructors
        #region IMethods
        
        public uint GenerateRequestId() {
            uint result;
            lock(this) {
                result = m_last;
                m_last++;
            }
            return result;
        }

        #endregion IMethods

    }
}