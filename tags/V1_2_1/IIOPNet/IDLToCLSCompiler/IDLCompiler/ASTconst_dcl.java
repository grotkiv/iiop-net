/* Generated By:JJTree: Do not edit this line. ASTconst_dcl.java */

package parser;

public class ASTconst_dcl extends SimpleNodeWithIdent {
  public ASTconst_dcl(int id) {
    super(id);
  }

  public ASTconst_dcl(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}
