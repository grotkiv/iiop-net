/* Generated By:JJTree: Do not edit this line. ASTvalue_element.cs */

using System;

namespace parser {

    public class ASTvalue_element : SimpleNode {
        
        #region IConstructors
        
        public ASTvalue_element(int id) : base(id) {
        }

        public ASTvalue_element(IDLParser p, int id) : base(p, id) {
        }
        
        #endregion IConstructors
        #region IMethods

        /** Accept the visitor. **/
        public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
            return visitor.visit(this, data);
        }
  
        public override string GetEmbedderDesc() {
            return ((SimpleNode)jjtGetParent()).GetEmbedderDesc();
        }
        
        #endregion IConstructors

    }

}

