/* Generated By:JJTree: Do not edit this line. ASTunion_type.java */

package parser;

public class ASTunion_type extends SimpleNodeWithIdent {
  public ASTunion_type(int id) {
    super(id);
  }

  public ASTunion_type(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}
