/* Generated By:JJTree: Do not edit this line. ASTenum_type.java */

package parser;

public class ASTenum_type extends SimpleNodeWithIdent {
  public ASTenum_type(int id) {
    super(id);
  }

  public ASTenum_type(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}