/* Generated By:JJTree: Do not edit this line. ASTspecification.cs */

using System;

namespace parser {

public class ASTspecification : SimpleNode {
  public ASTspecification(int id) : base(id) {
  }

  public ASTspecification(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}
