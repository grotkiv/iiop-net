/* Generated By:JJTree: Do not edit this line. ASTelement_spec.cs */

using System;

namespace parser {

public class ASTelement_spec : SimpleNode {
  public ASTelement_spec(int id) : base(id) {
  }

  public ASTelement_spec(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}
